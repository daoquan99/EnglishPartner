using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.Grammar.Common;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Application.Features.Grammar.Services;
using EnglishLiveTutor.Domain.Grammar;
using EnglishLiveTutor.Domain.Grammar.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.ImportGrammarExercisesJson;

public sealed class ImportGrammarExercisesJsonCommandHandler
    : ICommandHandler<ImportGrammarExercisesJsonCommand, GenerateGrammarExercisesResponse>
{
    private readonly IAppDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IGrammarExerciseOutputProcessor _processor;

    public ImportGrammarExercisesJsonCommandHandler(
        IAppDbContext db,
        IUnitOfWork uow,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IGrammarExerciseOutputProcessor processor)
    {
        _db = db;
        _uow = uow;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _processor = processor;
    }

    public async Task<GenerateGrammarExercisesResponse> HandleAsync(
        ImportGrammarExercisesJsonCommand command,
        CancellationToken ct = default)
    {
        string userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Bạn cần đăng nhập để nhập bài tập.");

        var now = _timeProvider.GetUtcNow();

        // Check if user already hit limits
        int past24hCount = await _db.GrammarExercises.AsNoTracking()
            .IgnoreQueryFilters()
            .CountAsync(e => (e.OwnerUserId == userId || e.AuthorUserId == userId) && e.CreatedAtUtc >= now.AddHours(-24), ct);

        if (past24hCount >= GrammarRules.MaxDailyExercisesPerUser)
        {
            throw new TooManyRequestsException("grammar_daily_quota_exceeded");
        }

        int currentPending = 0;
        if (command.Request.Visibility == GrammarExerciseVisibility.Public)
        {
            currentPending = await _db.GrammarExercises.AsNoTracking()
                .CountAsync(e => e.OwnerUserId == userId && !e.IsDeleted && e.Moderation == GrammarModerationStatus.Pending, ct);

            if (currentPending >= GrammarRules.MaxPendingExercisesPerUser)
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

        // Load existing hashes for deduplication
        var existingHashes = (await _db.GrammarExercises.AsNoTracking()
            .Where(e => e.StepId == step.Id && !e.IsDeleted &&
                        (e.Source == GrammarExerciseSource.Official ||
                         e.Moderation == GrammarModerationStatus.Approved ||
                         e.OwnerUserId == userId))
            .Select(e => e.ContentHash)
            .ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var (valid, rejected) = _processor.Process(
            command.Request.JsonText,
            command.Request.ExpectedItems,
            existingHashes);

        // Without ExpectedItems the processor does not cap the batch; enforce per-request and daily limits here.
        if (valid.Count > GrammarRules.MaxItemsPerRequest)
        {
            valid = valid.Take(GrammarRules.MaxItemsPerRequest).ToList();
        }

        if (past24hCount + valid.Count > GrammarRules.MaxDailyExercisesPerUser)
        {
            throw new TooManyRequestsException("grammar_daily_quota_exceeded");
        }

        if (command.Request.Visibility == GrammarExerciseVisibility.Public &&
            currentPending + valid.Count > GrammarRules.MaxPendingExercisesPerUser)
        {
            throw new ConflictException("grammar_pending_limit");
        }

        if (valid.Count == 0)
        {
            throw new UnprocessableEntityException(
                "grammar_generation_no_valid_exercises",
                new Dictionary<string, object?> { ["rejected"] = rejected });
        }

        // Recheck limits with valid count
        if (past24hCount + valid.Count > 40)
        {
            throw new TooManyRequestsException("grammar_daily_quota_exceeded");
        }

        if (command.Request.Visibility == GrammarExerciseVisibility.Public &&
            currentPending + valid.Count > 30)
        {
            throw new ConflictException("grammar_pending_limit");
        }

        var createdDtos = new List<GrammarMyExerciseDto>(valid.Count);
        foreach (var v in valid)
        {
            var exercise = GrammarExerciseEntity.CreateUserOwned(
                stepId: step.Id,
                ownerUserId: userId,
                source: GrammarExerciseSource.UserImport,
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
