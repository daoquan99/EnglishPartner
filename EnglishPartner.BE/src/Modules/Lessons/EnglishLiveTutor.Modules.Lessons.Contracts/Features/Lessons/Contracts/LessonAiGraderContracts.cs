using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Domain.Common.Exercises;

namespace EnglishLiveTutor.Application.Common.Interfaces;

public sealed record DeterministicGradeResult(
    bool IsCorrect,
    decimal Score100,
    decimal PointsEarned,
    decimal MaxPoints,
    string? Explanation,
    string? FeedbackJson = null
);

public interface IDeterministicExerciseGrader
{
    DeterministicGradeResult Grade(
        ExerciseType type,
        string contentJson,
        string? gradingDefinitionJson,
        string answerPayloadJson,
        decimal maxPoints);
}

public sealed record LessonAiCriterionScore(
    string CriterionCode,
    string Name,
    decimal Weight,
    int RawScore, // 0..4
    string Descriptor,
    string Feedback
);

public sealed record LessonAiGradingOutcome(
    bool IsAssessable,
    decimal Score100,
    decimal PointsEarned,
    List<LessonAiCriterionScore> CriteriaBreakdown,
    List<string> Strengths,
    List<string> Improvements,
    string? SuggestedAnswer
);

public interface ILessonAiGrader
{
    Task<LessonAiGradingOutcome> GradeSubmissionAsync(
        ExerciseType exerciseType,
        string taskInstructions,
        string cefrLevel,
        ScoringRubricSnapshot rubric,
        string learnerSubmissionPayloadJson,
        decimal maxPoints,
        string? taskContextJson = null,
        LessonAiExecutionConfigDto? aiConfig = null,
        CancellationToken ct = default);
}
