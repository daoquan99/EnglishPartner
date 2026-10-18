namespace EnglishLiveTutor.Domain.Lessons.Enums;

/// <summary>
/// Mode of grading applied to an exercise or sub-question.
/// </summary>
public enum GradingMode
{
    SelfAssessment = 1,
    Deterministic = 2,
    AiRubric = 3,
    Composite = 4
}
