using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.Grammar.Common;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Domain.Grammar;
using EnglishLiveTutor.Domain.Grammar.Enums;
using EnglishLiveTutor.Domain.Common.Exercises;
using EnglishLiveTutor.Application.Modules.Grammar;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands;

public sealed record CompleteGrammarAttemptCommand(Guid AttemptId) : ICommand<GrammarAttemptResultDto>;

public sealed class CompleteGrammarAttemptCommandHandler : ICommandHandler<CompleteGrammarAttemptCommand, GrammarAttemptResultDto>
{
    private readonly IGrammarDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteGrammarAttemptCommandHandler(
        IGrammarDbContext db,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<GrammarAttemptResultDto> HandleAsync(CompleteGrammarAttemptCommand command, CancellationToken ct = default)
    {
        string? userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("Authentication required to complete grammar attempt.");
        }

        var attempt = await _db.GrammarAttempts
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == command.AttemptId, ct);

        if (attempt == null || !string.Equals(attempt.UserId, userId, StringComparison.Ordinal))
        {
            throw new NotFoundException($"Grammar attempt with ID '{command.AttemptId}' was not found.");
        }

        if (attempt.CompletedAtUtc.HasValue)
        {
            throw new ConflictException("Attempt has already been completed.");
        }

        var exerciseIds = attempt.ExerciseIds;
        var exercises = await _db.GrammarExercises
            .AsNoTracking()
            .Where(e => exerciseIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, ct);

        int gradedCount = 0;
        decimal totalScore = 0m;

        foreach (var exId in exerciseIds)
        {
            // Soft-deleted exercises are excluded from denominator and numerator
            if (!exercises.TryGetValue(exId, out var ex) || ex.IsDeleted)
            {
                continue;
            }

            var answer = attempt.Answers.FirstOrDefault(a => a.ExerciseId == exId);
            if (answer == null)
            {
                // Not submitted -> counts as 0 in numerator, counts in denominator
                totalScore += 0m;
                gradedCount++;
                continue;
            }

            if (answer.Status is "SelfAssessed" or "NotAssessable" or "FailedGrading")
            {
                // Exclude from denominator
                continue;
            }

            totalScore += answer.Score100 ?? 0m;
            gradedCount++;
        }

        decimal finalScore = gradedCount > 0
            ? Math.Round(totalScore / gradedCount, 2, MidpointRounding.AwayFromZero)
            : 0m;

        var now = _timeProvider.GetUtcNow();
        attempt.Complete(finalScore, now);

        bool passed = false;
        bool? levelPassed = null;
        var unlockedStepCodes = new List<string>();

        // Load all published steps in roadmap order for unlocking logic
        var sortedSteps = await GrammarRoadmapCalculator.GetSortedPublishedStepsAsync(_db, ct);

        UserGrammarStepProgressEntity? newlyCreatedProgress = null;
        List<GrammarStepEntity>? levelSteps = null;
        List<Guid>? levelStepIds = null;
        var newlyCreatedLevelProgresses = new List<UserGrammarStepProgressEntity>();

        if (attempt.Kind == GrammarAttemptKind.StepCore)
        {
            if (!attempt.StepId.HasValue)
            {
                throw new InvalidOperationException("StepCore attempt must have a StepId.");
            }

            bool passByScore = finalScore >= GrammarRules.StepPassThreshold;
            bool hasAiRubric = exercises.Values.Any(e => !e.IsDeleted && e.GradingMode == GradingMode.AiRubric);
            bool meetsAiRubricCondition = !hasAiRubric ||
                attempt.Answers.Any(a => exercises.TryGetValue(a.ExerciseId, out var ex) && !ex.IsDeleted && ex.GradingMode == GradingMode.AiRubric);

            passed = (gradedCount > 0) && passByScore && meetsAiRubricCondition;

            var progress = await _db.UserGrammarStepProgress
                .FirstOrDefaultAsync(p => p.UserId == userId && p.StepId == attempt.StepId.Value, ct);

            if (progress == null)
            {
                progress = UserGrammarStepProgressEntity.Create(userId, attempt.StepId.Value, now);
                _db.UserGrammarStepProgress.Add(progress);
                newlyCreatedProgress = progress;
            }

            if (passed)
            {
                progress.RecordScore(finalScore, GrammarRules.StepPassThreshold, now);

                // Find next published step in roadmap
                int currentStepIdx = sortedSteps.FindIndex(s => s.Id == attempt.StepId.Value);
                if (currentStepIdx >= 0 && currentStepIdx < sortedSteps.Count - 1)
                {
                    unlockedStepCodes.Add(sortedSteps[currentStepIdx + 1].Code);
                }
            }
            else
            {
                // Record score without passing
                progress.RecordScore(finalScore, 101m, now);
            }
        }
        else if (attempt.Kind == GrammarAttemptKind.LevelTest)
        {
            if (gradedCount > 0 && finalScore >= GrammarRules.LevelTestPassThreshold)
            {
                levelPassed = true;
                passed = true;

                levelSteps = sortedSteps
                    .Where(s => string.Equals(s.CefrLevel, attempt.CefrLevel, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                levelStepIds = levelSteps.Select(s => s.Id).ToList();

                var existingProgresses = await _db.UserGrammarStepProgress
                    .Where(p => p.UserId == userId && levelStepIds.Contains(p.StepId))
                    .ToDictionaryAsync(p => p.StepId, ct);

                foreach (var step in levelSteps)
                {
                    if (existingProgresses.TryGetValue(step.Id, out var prog))
                    {
                        prog.MarkPassedByLevelTest(now);
                    }
                    else
                    {
                        var newProg = UserGrammarStepProgressEntity.Create(userId, step.Id, now);
                        newProg.MarkPassedByLevelTest(now);
                        _db.UserGrammarStepProgress.Add(newProg);
                        newlyCreatedLevelProgresses.Add(newProg);
                    }

                    unlockedStepCodes.Add(step.Code);
                }

                // Unlock step 1 of next level if there is a next level
                int currentLevelIndex = Array.IndexOf(GrammarRoadmapCalculator.LevelOrder, attempt.CefrLevel.ToUpperInvariant());
                if (currentLevelIndex >= 0 && currentLevelIndex < GrammarRoadmapCalculator.LevelOrder.Length - 1)
                {
                    string nextLevel = GrammarRoadmapCalculator.LevelOrder[currentLevelIndex + 1];
                    var nextLevelFirstStep = sortedSteps
                        .FirstOrDefault(s => string.Equals(s.CefrLevel, nextLevel, StringComparison.OrdinalIgnoreCase));

                    if (nextLevelFirstStep != null && !unlockedStepCodes.Contains(nextLevelFirstStep.Code))
                    {
                        unlockedStepCodes.Add(nextLevelFirstStep.Code);
                    }
                }
            }
            else
            {
                levelPassed = false;
                passed = false;
            }
        }
        else // Practice
        {
            passed = false;
            levelPassed = null;
        }

        try
        {
            await _unitOfWork.CommitChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("grammar_conflict");
        }
        catch (DbUpdateException ex) when (DbUpdateExceptionClassifier.IsUniqueConstraintViolation(ex))
        {
            // Handle race condition on UserGrammarStepProgress unique constraint (UserId, StepId)
            if (attempt.Kind == GrammarAttemptKind.StepCore && newlyCreatedProgress != null && attempt.StepId.HasValue)
            {
                _db.UserGrammarStepProgress.Entry(newlyCreatedProgress).State = EntityState.Detached;

                var existingProgress = await _db.UserGrammarStepProgress
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.StepId == attempt.StepId.Value, ct);

                if (existingProgress != null)
                {
                    if (passed)
                    {
                        existingProgress.RecordScore(finalScore, GrammarRules.StepPassThreshold, now);
                    }
                    else
                    {
                        existingProgress.RecordScore(finalScore, 101m, now);
                    }

                    try
                    {
                        await _unitOfWork.CommitChangesAsync(ct);
                    }
                    catch
                    {
                        throw new ConflictException("grammar_conflict");
                    }
                }
                else
                {
                    throw new ConflictException("grammar_conflict");
                }
            }
            else if (attempt.Kind == GrammarAttemptKind.LevelTest && levelSteps != null && levelStepIds != null)
            {
                // Detach newly added progress entities
                foreach (var entry in newlyCreatedLevelProgresses)
                {
                    _db.UserGrammarStepProgress.Entry(entry).State = EntityState.Detached;
                }

                var reloadedProgresses = await _db.UserGrammarStepProgress
                    .Where(p => p.UserId == userId && levelStepIds.Contains(p.StepId))
                    .ToDictionaryAsync(p => p.StepId, ct);

                foreach (var step in levelSteps)
                {
                    if (reloadedProgresses.TryGetValue(step.Id, out var prog))
                    {
                        prog.MarkPassedByLevelTest(now);
                    }
                    else
                    {
                        var newProg = UserGrammarStepProgressEntity.Create(userId, step.Id, now);
                        newProg.MarkPassedByLevelTest(now);
                        _db.UserGrammarStepProgress.Add(newProg);
                        newlyCreatedLevelProgresses.Add(newProg);
                    }
                }

                try
                {
                    await _unitOfWork.CommitChangesAsync(ct);
                }
                catch
                {
                    throw new ConflictException("grammar_conflict");
                }
            }
            else
            {
                throw new ConflictException("grammar_conflict");
            }
        }

        return new GrammarAttemptResultDto(
            Score100: finalScore,
            Passed: passed,
            UnlockedStepCodes: unlockedStepCodes,
            LevelPassed: levelPassed
        );
    }
}
