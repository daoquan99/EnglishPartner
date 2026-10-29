using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.Grammar.Common;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Application.Features.Grammar.Services;
using EnglishLiveTutor.Application.Features.Vocabulary;
using EnglishLiveTutor.Domain.Grammar;
using EnglishLiveTutor.Domain.Grammar.Enums;
using EnglishLiveTutor.Domain.Lessons.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands;

public sealed record StartGrammarAttemptCommand(
    GrammarAttemptKind Kind,
    string? StepCode = null,
    string? CefrLevel = null,
    // Start a fresh attempt instead of resuming the unfinished one; the old attempt and its answers are kept.
    bool ForceNew = false
) : ICommand<GrammarAttemptDto>;

public sealed class StartGrammarAttemptCommandHandler : ICommandHandler<StartGrammarAttemptCommand, GrammarAttemptDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGrammarLearnerContentSanitizer _sanitizer;

    public StartGrammarAttemptCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork,
        IGrammarLearnerContentSanitizer sanitizer)
    {
        _db = db;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
        _sanitizer = sanitizer;
    }

    public async Task<GrammarAttemptDto> HandleAsync(StartGrammarAttemptCommand command, CancellationToken ct = default)
    {
        string? userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("Authentication required to start grammar attempt.");
        }

        var now = _timeProvider.GetUtcNow();
        var cutoff = now.AddHours(-24);

        GrammarStepEntity? step = null;
        string cefrLevel;

        if (command.Kind is GrammarAttemptKind.StepCore or GrammarAttemptKind.Practice)
        {
            if (string.IsNullOrWhiteSpace(command.StepCode))
            {
                throw new ArgumentException("StepCode is required for StepCore and Practice attempts.", nameof(command.StepCode));
            }

            string normalizedCode = command.StepCode.Trim().ToUpperInvariant();
            step = await _db.GrammarSteps
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Code == normalizedCode && s.IsPublished && !s.IsDeleted, ct);

            if (step == null)
            {
                throw new NotFoundException($"Grammar step '{normalizedCode}' was not found or is not published.");
            }

            cefrLevel = step.CefrLevel;

            // Check if active uncompleted attempt started within 24h already exists
            var existingAttempt = await _db.GrammarAttempts
                .Include(a => a.Answers)
                .Where(a => a.UserId == userId &&
                            a.Kind == command.Kind &&
                            a.StepId == step.Id &&
                            a.CompletedAtUtc == null &&
                            a.StartedAtUtc >= cutoff)
                .OrderByDescending(a => a.StartedAtUtc)
                .FirstOrDefaultAsync(ct);

            if (existingAttempt != null && !command.ForceNew)
            {
                var validCount = await _db.GrammarExercises
                    .AsNoTracking()
                    .CountAsync(e => existingAttempt.ExerciseIds.Contains(e.Id) && !e.IsDeleted, ct);

                if (validCount == existingAttempt.ExerciseIds.Count)
                {
                    return await GrammarAttemptDtoMapper.MapToDtoAsync(_db, _sanitizer, existingAttempt, userId, ct);
                }
            }
        }
        else if (command.Kind == GrammarAttemptKind.LevelTest)
        {
            if (string.IsNullOrWhiteSpace(command.CefrLevel))
            {
                throw new ArgumentException("CefrLevel is required for LevelTest attempts.", nameof(command.CefrLevel));
            }

            cefrLevel = command.CefrLevel.Trim().ToUpperInvariant();
            if (!GrammarStepEntity.Levels.Contains(cefrLevel))
            {
                throw new ArgumentException($"CefrLevel must be one of {string.Join(", ", GrammarStepEntity.Levels)}.", nameof(command.CefrLevel));
            }

            // Check if active uncompleted attempt started within 24h already exists
            var existingAttempt = await _db.GrammarAttempts
                .Include(a => a.Answers)
                .Where(a => a.UserId == userId &&
                            a.Kind == command.Kind &&
                            a.CefrLevel == cefrLevel &&
                            a.CompletedAtUtc == null &&
                            a.StartedAtUtc >= cutoff)
                .OrderByDescending(a => a.StartedAtUtc)
                .FirstOrDefaultAsync(ct);

            if (existingAttempt != null && !command.ForceNew)
            {
                var validCount = await _db.GrammarExercises
                    .AsNoTracking()
                    .CountAsync(e => existingAttempt.ExerciseIds.Contains(e.Id) && !e.IsDeleted, ct);

                if (validCount == existingAttempt.ExerciseIds.Count)
                {
                    return await GrammarAttemptDtoMapper.MapToDtoAsync(_db, _sanitizer, existingAttempt, userId, ct);
                }
            }
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(command.Kind), "Unsupported attempt kind.");
        }

        List<Guid> exerciseIds;

        if (command.Kind == GrammarAttemptKind.StepCore)
        {
            var allSteps = await GrammarRoadmapCalculator.GetSortedPublishedStepsAsync(_db, ct);

            var userProgresses = await _db.UserGrammarStepProgress
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .ToListAsync(ct);

            var (status, _, _) = GrammarRoadmapCalculator.GetStepStatus(step!.Code, allSteps, userProgresses);
            if (status == "Locked")
            {
                throw new ForbiddenException("grammar_step_locked");
            }

            var coreExercises = await _db.GrammarExercises
                .AsNoTracking()
                .Where(e => e.StepId == step.Id && !e.IsDeleted && e.Source == GrammarExerciseSource.Official && e.IsCore)
                .OrderBy(e => e.Difficulty)
                .ThenBy(e => e.Key)
                .Select(e => new { e.Id, e.GradingMode })
                .ToListAsync(ct);

            if (coreExercises.Count == 0 || !coreExercises.Any(e => e.GradingMode is GradingMode.Deterministic or GradingMode.Composite))
            {
                throw new ConflictException("grammar_step_has_no_core_exercises");
            }

            exerciseIds = coreExercises.Select(e => e.Id).ToList();
        }
        else if (command.Kind == GrammarAttemptKind.Practice)
        {
            var baseQuery = _db.GrammarExercises
                .AsNoTracking()
                .Where(e => e.StepId == step!.Id && !e.IsDeleted &&
                           (e.Source == GrammarExerciseSource.Official ||
                            e.Moderation == GrammarModerationStatus.Approved ||
                            e.OwnerUserId == userId));

            try
            {
                exerciseIds = await baseQuery
                    .OrderBy(_ => Guid.NewGuid())
                    .Take(GrammarRules.PracticeMaxItems)
                    .Select(e => e.Id)
                    .ToListAsync(ct);
            }
            catch (InvalidOperationException)
            {
                var allIds = await baseQuery.Select(e => e.Id).ToListAsync(ct);
                exerciseIds = allIds.OrderBy(_ => Random.Shared.Next()).Take(GrammarRules.PracticeMaxItems).ToList();
            }

            if (exerciseIds.Count == 0)
            {
                throw new ConflictException("grammar_step_has_no_exercises");
            }
        }
        else // LevelTest
        {
            var allSteps = await GrammarRoadmapCalculator.GetSortedPublishedStepsAsync(_db, ct);

            var userProgresses = await _db.UserGrammarStepProgress
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .ToListAsync(ct);

            bool canTake = GrammarRoadmapCalculator.CanTakeLevelTest(cefrLevel, allSteps, userProgresses);
            if (!canTake)
            {
                throw new ForbiddenException("grammar_level_test_locked");
            }

            var stepIdsInLevel = allSteps
                .Where(s => string.Equals(s.CefrLevel, cefrLevel, StringComparison.OrdinalIgnoreCase))
                .Select(s => s.Id)
                .ToList();

            var coreExercises = await _db.GrammarExercises
                .AsNoTracking()
                .Where(e => stepIdsInLevel.Contains(e.StepId) && !e.IsDeleted && e.Source == GrammarExerciseSource.Official && e.IsCore)
                .Select(e => new { e.Id, e.StepId })
                .ToListAsync(ct);

            if (coreExercises.Count < 10)
            {
                throw new ConflictException("grammar_level_test_not_enough_exercises");
            }

            int targetCount = coreExercises.Count < 20
                ? coreExercises.Count
                : Math.Min(coreExercises.Count, 30);

            if (coreExercises.Count >= 20 && targetCount < 20)
            {
                targetCount = 20;
            }

            var byStep = coreExercises.GroupBy(e => e.StepId).ToList();
            var chosen = new List<Guid>();
            var remainingPool = new List<Guid>();

            foreach (var group in byStep)
            {
                var shuffledGroup = group.Select(x => x.Id).OrderBy(_ => Random.Shared.Next()).ToList();
                chosen.Add(shuffledGroup[0]);
                remainingPool.AddRange(shuffledGroup.Skip(1));
            }

            if (chosen.Count > targetCount)
            {
                chosen = chosen.OrderBy(_ => Random.Shared.Next()).Take(targetCount).ToList();
            }
            else
            {
                var additionalNeeded = targetCount - chosen.Count;
                if (additionalNeeded > 0)
                {
                    var added = remainingPool.OrderBy(_ => Random.Shared.Next()).Take(additionalNeeded);
                    chosen.AddRange(added);
                }
            }

            exerciseIds = chosen.OrderBy(_ => Random.Shared.Next()).ToList();
        }

        var newAttempt = GrammarAttemptEntity.Start(
            userId: userId,
            kind: command.Kind,
            stepId: step?.Id,
            cefrLevel: cefrLevel,
            exerciseIds: exerciseIds,
            startedAtUtc: now);

        _db.GrammarAttempts.Add(newAttempt);

        try
        {
            await _unitOfWork.CommitChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("grammar_conflict");
        }
        catch (DbUpdateException ex) when (VocabularyGenerationWriter.IsUniqueConstraintViolation(ex))
        {
            throw new ConflictException("grammar_conflict");
        }

        return await GrammarAttemptDtoMapper.MapToDtoAsync(_db, _sanitizer, newAttempt, userId, ct);
    }
}
