using System.Text.Json.Nodes;

namespace EnglishLiveTutor.Application.Common.Models;

public sealed record StartLessonAttemptRequest(
    Guid LessonId
);

public sealed record LessonAiExecutionConfigDto(
    string? ApiKey = null,
    string? AssessmentModelCode = null
);

public sealed record SubmitExerciseAnswerRequest(
    Guid ExerciseId,
    Guid? SubQuestionId,
    int AttemptRevision,
    JsonNode AnswerPayload,
    LessonAiExecutionConfigDto? AiConfig = null
);

public sealed record RetryGradingRequest(
    int AttemptRevision,
    LessonAiExecutionConfigDto? AiConfig = null
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

public sealed record DictationWordSubstitutionDto(
    string Expected,
    string Actual
);

public sealed record DictationAiReviewDto(
    Guid ReviewId,
    Guid AnswerId,
    string Status,
    string? Feedback,
    List<string>? MissingWords,
    List<string>? ExtraWords,
    List<DictationWordSubstitutionDto>? WordSubstitutions,
    string? ExplanationsVi,
    List<string>? ActionableImprovements,
    string? ErrorMessage = null,
    DateTimeOffset? ReviewedAtUtc = null
);

public sealed record RequestDictationAiReviewRequest(
    LessonAiExecutionConfigDto? AiConfig = null,
    string? ExpectedSubmissionHash = null
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
    AttemptProgressDto AttemptProgress,
    string? ErrorMessage = null,
    JsonNode? AnswerPayload = null,
    DictationAiReviewDto? AiReview = null
);

public sealed record SubmittedAnswerSummaryDto(
    Guid AnswerId,
    string Status,
    decimal? Score100,
    decimal? PointsEarned,
    decimal MaxPoints,
    string? Feedback,
    JsonNode? AnswerPayload = null,
    string? ErrorMessage = null,
    DictationAiReviewDto? AiReview = null,
    List<CriterionScoreBreakdownDto>? CriteriaBreakdown = null,
    Guid? ExerciseId = null,
    Guid? SubQuestionId = null
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
