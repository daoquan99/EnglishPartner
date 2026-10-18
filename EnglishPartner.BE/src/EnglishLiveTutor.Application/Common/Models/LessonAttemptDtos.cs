using System.Text.Json.Nodes;

namespace EnglishLiveTutor.Application.Common.Models;

public sealed record StartLessonAttemptRequest(
    Guid LessonId
);

public sealed record SubmitExerciseAnswerRequest(
    Guid ExerciseId,
    Guid? SubQuestionId,
    int AttemptRevision,
    JsonNode AnswerPayload
);

public sealed record CriterionScoreBreakdownDto(
    string CriterionCode,
    string Name,
    decimal Weight,
    int RawScore,
    string Descriptor,
    string Feedback
);

public sealed record AttemptProgressDto(
    int CompletedExercisesCount,
    int TotalExercisesCount,
    int PendingGradingCount,
    int FailedGradingCount,
    decimal CurrentSubtotalPoints,
    decimal TotalMaxPoints,
    decimal? FinalScore100
);

public sealed record ExerciseAnswerResultDto(
    Guid AnswerId,
    Guid AttemptId,
    int AttemptRevision,
    Guid ExerciseId,
    Guid? SubQuestionId,
    string Status,
    string GradingMode,
    decimal? Score100,
    decimal? PointsEarned,
    decimal MaxPoints,
    string? RubricCode,
    int? RubricVersion,
    List<CriterionScoreBreakdownDto>? CriteriaBreakdown,
    List<string>? Strengths,
    List<string>? Improvements,
    string? SuggestedAnswer,
    AttemptProgressDto AttemptProgress
);

public sealed record SubmittedAnswerSummaryDto(
    Guid AnswerId,
    string Status,
    decimal? Score100,
    decimal? PointsEarned,
    decimal MaxPoints,
    string? Feedback
);

public sealed record LessonAttemptResumeDto(
    Guid AttemptId,
    Guid LessonId,
    string LessonTitle,
    int PinnedLessonVersion,
    int AttemptRevision,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    AttemptProgressDto Progress,
    IReadOnlyList<ExerciseLearnerDto> Exercises,
    Dictionary<string, SubmittedAnswerSummaryDto> SubmittedAnswers
);

public sealed record LessonAttemptSummaryDto(
    Guid AttemptId,
    Guid LessonId,
    string LessonTitle,
    int PinnedLessonVersion,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    decimal CurrentSubtotalPoints,
    decimal TotalMaxPoints,
    decimal? FinalScore100
);
