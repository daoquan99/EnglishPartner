using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Common.Validation;
using EnglishLiveTutor.Application.Features.Grammar.Common;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Application.Features.Grammar.Rules;
using EnglishLiveTutor.Domain.Grammar.Enums;
using FluentValidation.Results;
using EnglishLiveTutor.Application.Modules.Grammar;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.UpdateMyGrammarExercise;

public sealed class UpdateMyGrammarExerciseCommandHandler
    : ICommandHandler<UpdateMyGrammarExerciseCommand, GrammarMyExerciseDto>
{
    private readonly IGrammarDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public UpdateMyGrammarExerciseCommandHandler(
        IGrammarDbContext db,
        IUnitOfWork uow,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _db = db;
        _uow = uow;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<GrammarMyExerciseDto> HandleAsync(
        UpdateMyGrammarExerciseCommand command,
        CancellationToken ct = default)
    {
        string userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Bạn cần đăng nhập để sửa bài tập.");

        var exercise = await _db.GrammarExercises
            .FirstOrDefaultAsync(e => e.Id == command.Id && !e.IsDeleted, ct);

        // Rule 2: Not found or not owner -> 404
        if (exercise == null || !string.Equals(exercise.OwnerUserId, userId, StringComparison.Ordinal))
        {
            throw new NotFoundException($"Exercise '{command.Id}' không tồn tại hoặc bạn không có quyền truy cập.");
        }

        string contentJson = command.Request.Content.GetRawText();
        string? gradingJson = command.Request.Grading.HasValue && command.Request.Grading.Value.ValueKind != System.Text.Json.JsonValueKind.Null
            ? command.Request.Grading.Value.GetRawText()
            : null;

        var issues = GrammarExerciseRules.Validate(
            exercise.Type,
            contentJson,
            gradingJson,
            exercise.MaxPoints,
            exercise.GradingMode);

        if (issues.Count > 0)
        {
            var failures = issues.Select(i => new ValidationFailure(i.Path, i.Message)).ToList();
            throw new ValidationException(failures);
        }

        string newHash = GrammarContentHasher.ComputeHash(exercise.Type, contentJson, gradingJson);

        bool isDuplicate = await _db.GrammarExercises.AsNoTracking()
            .AnyAsync(o => o.Id != exercise.Id &&
                           o.StepId == exercise.StepId &&
                           !o.IsDeleted &&
                           (o.Source == GrammarExerciseSource.Official ||
                            o.Moderation == GrammarModerationStatus.Approved ||
                            o.OwnerUserId == userId) &&
                           o.ContentHash == newHash, ct);

        if (isDuplicate)
        {
            throw new ConflictException("grammar_duplicate_content");
        }

        if (exercise.Visibility == GrammarExerciseVisibility.Public &&
            exercise.Moderation != GrammarModerationStatus.Pending)
        {
            int currentPending = await _db.GrammarExercises.AsNoTracking()
                .CountAsync(e => e.OwnerUserId == userId && e.Moderation == GrammarModerationStatus.Pending, ct);
            if (currentPending >= GrammarRules.MaxPendingExercisesPerUser)
            {
                throw new ConflictException("grammar_pending_limit");
            }
        }

        var now = _timeProvider.GetUtcNow();
        exercise.EditContent(
            type: exercise.Type,
            difficulty: command.Request.Difficulty,
            title: command.Request.Title,
            instructions: command.Request.Instructions,
            contentJson: contentJson,
            gradingDefinitionJson: gradingJson,
            maxPoints: exercise.MaxPoints,
            gradingMode: exercise.GradingMode,
            contentHash: newHash,
            now: now,
            focus: command.Request.Focus);

        await _uow.CommitChangesAsync(ct);

        string stepCode = await _db.GrammarSteps.AsNoTracking()
            .Where(s => s.Id == exercise.StepId)
            .Select(s => s.Code)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        return GrammarExerciseDtoMapper.MapToMyExerciseDto(exercise, stepCode);
    }
}
