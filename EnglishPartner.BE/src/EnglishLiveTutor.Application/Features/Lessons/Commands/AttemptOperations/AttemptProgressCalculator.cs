using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.LessonAttempts;
using EnglishLiveTutor.Domain.LessonAttempts.Enums;
using EnglishLiveTutor.Domain.Lessons.Entities;

namespace EnglishLiveTutor.Application.Features.Lessons.Commands.AttemptOperations;

public static class AttemptProgressCalculator
{
    public static AttemptProgressDto CalculateProgress(LessonAttemptEntity attempt, IEnumerable<ExerciseEntity> exercises)
    {
        int totalExercises = exercises.Count();
        int completedCount = attempt.Answers.Count(a => a.Status == SubmissionGradingStatus.Completed);
        int pendingCount = attempt.Answers.Count(a => a.Status is SubmissionGradingStatus.Pending or SubmissionGradingStatus.Grading);
        int failedCount = attempt.Answers.Count(a => a.Status is SubmissionGradingStatus.Failed or SubmissionGradingStatus.NotAssessable);

        return new AttemptProgressDto(
            CompletedExercisesCount: completedCount,
            TotalExercisesCount: totalExercises,
            PendingGradingCount: pendingCount,
            FailedGradingCount: failedCount,
            CurrentSubtotalPoints: attempt.CurrentSubtotalPoints,
            TotalMaxPoints: attempt.TotalMaxPoints,
            FinalScore100: attempt.FinalScore100);
    }
}
