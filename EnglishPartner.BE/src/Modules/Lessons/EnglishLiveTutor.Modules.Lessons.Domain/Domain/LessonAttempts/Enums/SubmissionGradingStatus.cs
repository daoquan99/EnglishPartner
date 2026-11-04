namespace EnglishLiveTutor.Domain.LessonAttempts.Enums;

/// <summary>
/// Status of an exercise answer submission during grading lifecycle.
/// </summary>
public enum SubmissionGradingStatus
{
    Pending = 1,
    Grading = 2,
    Completed = 3,
    NotAssessable = 4,
    Failed = 5
}
