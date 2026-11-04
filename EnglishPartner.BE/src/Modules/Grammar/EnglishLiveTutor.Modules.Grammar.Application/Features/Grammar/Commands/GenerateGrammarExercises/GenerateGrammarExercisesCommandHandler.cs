using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.Grammar.Common;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Application.Features.Grammar.Ports;
using EnglishLiveTutor.Application.Features.Grammar.Services;
using EnglishLiveTutor.Domain.Grammar;
using EnglishLiveTutor.Domain.Grammar.Enums;
using EnglishLiveTutor.Application.Modules.Grammar;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.GenerateGrammarExercises;

public sealed class GenerateGrammarExercisesCommandHandler
    : ICommandHandler<GenerateGrammarExercisesCommand, GenerateGrammarExercisesResponse>
{
    private readonly IGrammarDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IGrammarExercisePromptContextLoader _contextLoader;
    private readonly IGrammarExercisePromptBuilder _promptBuilder;
    private readonly IGrammarExerciseAiGenerator _aiGenerator;
    private readonly IGrammarExerciseOutputProcessor _processor;

    public GenerateGrammarExercisesCommandHandler(
        IGrammarDbContext db,
        IUnitOfWork uow,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IGrammarExercisePromptContextLoader contextLoader,
        IGrammarExercisePromptBuilder promptBuilder,
        IGrammarExerciseAiGenerator aiGenerator,
        IGrammarExerciseOutputProcessor processor)
    {
        _db = db;
        _uow = uow;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _contextLoader = contextLoader;
        _promptBuilder = promptBuilder;
        _aiGenerator = aiGenerator;
        _processor = processor;
    }

    public async Task<GenerateGrammarExercisesResponse> HandleAsync(
        GenerateGrammarExercisesCommand command,
        CancellationToken ct = default)
    {
        string userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Bạn cần đăng nhập để tạo bài tập.");

        var now = _timeProvider.GetUtcNow();
        int totalRequested = command.Request.Items.Sum(i => i.Count);

        // Daily quota check: <= 40 exercises created in past 24 hours
        int past24hCount = await _db.GrammarExercises.AsNoTracking()
            .IgnoreQueryFilters()
            .CountAsync(e => (e.OwnerUserId == userId || e.AuthorUserId == userId) && e.CreatedAtUtc >= now.AddHours(-24), ct);

        if (past24hCount + totalRequested > GrammarRules.MaxDailyExercisesPerUser)
        {
            throw new TooManyRequestsException("grammar_daily_quota_exceeded");
        }

        // Pending limit check: <= 30 pending exercises per user
        if (command.Request.Visibility == GrammarExerciseVisibility.Public)
        {
            int currentPending = await _db.GrammarExercises.AsNoTracking()
                .CountAsync(e => e.OwnerUserId == userId && !e.IsDeleted && e.Moderation == GrammarModerationStatus.Pending, ct);

            if (currentPending + totalRequested > GrammarRules.MaxPendingExercisesPerUser)
            {
                throw new ConflictException("grammar_pending_limit");
            }
        }

        string normalizedCode = command.StepCode.Trim().ToUpperInvariant();
        var step = await _db.GrammarSteps.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Code == normalizedCode && !s.IsDeleted, ct);

        if (step == null)
        {
            throw new NotFoundException($"Step '{command.StepCode}' không tồn tại.");
        }

        // 1. Build prompt via context loader and builder
        var promptInput = await _contextLoader.LoadContextAsync(
            command.StepCode,
            command.Request.Items,
            command.Request.LearnerNote,
            ct);

        string promptText = _promptBuilder.Build(promptInput);

        // 2. Call AI provider outside transaction
        string rawResponse = await _aiGenerator.GenerateRawAsync(
            promptText,
            command.Request.AiConfig ?? new GrammarAiExecutionConfig(),
            ct);

        // 3. Load existing hashes for deduplication
        var existingHashes = (await _db.GrammarExercises.AsNoTracking()
            .Where(e => e.StepId == step.Id && !e.IsDeleted &&
                        (e.Source == GrammarExerciseSource.Official ||
                         e.Moderation == GrammarModerationStatus.Approved ||
                         e.OwnerUserId == userId))
            .Select(e => e.ContentHash)
            .ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 4. Process AI output
        var (valid, rejected) = _processor.Process(rawResponse, command.Request.Items, existingHashes);

        if (valid.Count == 0)
        {
            throw new UnprocessableEntityException(
                "grammar_generation_no_valid_exercises",
                new Dictionary<string, object?> { ["rejected"] = rejected });
        }

        // 5. Persist valid exercises
        var createdDtos = new List<GrammarMyExerciseDto>(valid.Count);
        foreach (var v in valid)
        {
            var exercise = GrammarExerciseEntity.CreateUserOwned(
                stepId: step.Id,
                ownerUserId: userId,
                source: GrammarExerciseSource.UserAi,
                visibility: command.Request.Visibility,
                type: v.Type,
                difficulty: v.Difficulty,
                title: v.Title,
                instructions: v.Instructions,
                contentJson: v.ContentJson,
                gradingDefinitionJson: v.GradingDefinitionJson,
                maxPoints: v.MaxPoints,
                gradingMode: v.GradingMode,
                contentHash: v.ContentHash,
                now: now,
                focus: v.Focus);

            _db.GrammarExercises.Add(exercise);
            createdDtos.Add(GrammarExerciseDtoMapper.MapToMyExerciseDto(exercise, step.Code));
        }

        await _uow.CommitChangesAsync(ct);

        return new GenerateGrammarExercisesResponse(createdDtos, rejected);
    }
}
