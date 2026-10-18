using System.Text.Json.Nodes;

namespace EnglishLiveTutor.Application.Common.Models;

public sealed record LessonExerciseTypeCatalogDto(
    string ExerciseType,
    int TypeOrdinal,
    string Group,
    string LabelVi,
    string LabelEn,
    string DefaultGradingMode,
    bool RequiresMedia
);

public sealed record LessonSummaryDto(
    Guid Id,
    string Title,
    string CefrLevel,
    string Topic,
    string Content,
    int EstimatedMinutes,
    string Status,
    int Version,
    int ExerciseCount,
    decimal TotalMaxPoints,
    DateTimeOffset? PublishedAtUtc,
    string? RowVersion
);

public sealed record ExerciseAuthorDto(
    Guid Id,
    string Key,
    string Type,
    string Title,
    string Instructions,
    int Order,
    decimal MaxPoints,
    string GradingMode,
    List<string>? SkillTags,
    JsonNode? Content,
    JsonNode? Grading
);

public sealed record ExerciseLearnerDto(
    Guid Id,
    string Key,
    string Type,
    string Title,
    string Instructions,
    int Order,
    decimal MaxPoints,
    string GradingMode,
    JsonNode? Content
);

public sealed record LessonDetailAuthorDto(
    Guid Id,
    string Title,
    string CefrLevel,
    string Topic,
    List<string> LearningObjectives,
    List<string> TargetVocabulary,
    List<string> TargetGrammar,
    string Content,
    int EstimatedMinutes,
    string Status,
    int Version,
    string? RowVersion,
    DateTimeOffset? PublishedAtUtc,
    IReadOnlyList<ExerciseAuthorDto> Exercises
);

public sealed record LessonDetailLearnerDto(
    Guid Id,
    string Title,
    string CefrLevel,
    string Topic,
    List<string> LearningObjectives,
    List<string> TargetVocabulary,
    List<string> TargetGrammar,
    string Content,
    int EstimatedMinutes,
    int Version,
    IReadOnlyList<ExerciseLearnerDto> Exercises
);

public sealed record CreateLessonDraftRequest(
    string Title,
    string CefrLevel,
    string Topic,
    string? Content,
    int EstimatedMinutes,
    List<string>? LearningObjectives,
    List<string>? TargetVocabulary,
    List<string>? TargetGrammar
);

public sealed record UpdateLessonDraftRequest(
    string Title,
    string CefrLevel,
    string Topic,
    string? Content,
    int EstimatedMinutes,
    List<string>? LearningObjectives,
    List<string>? TargetVocabulary,
    List<string>? TargetGrammar,
    string? RowVersion
);

public sealed record AddExerciseRequest(
    string Key,
    string Type,
    string Title,
    string Instructions,
    decimal MaxPoints,
    string GradingMode,
    JsonNode Content,
    JsonNode? Grading = null,
    List<string>? SkillTags = null,
    int? Order = null
);

public sealed record UpdateExerciseRequest(
    string Key,
    string Type,
    string Title,
    string Instructions,
    decimal MaxPoints,
    string GradingMode,
    JsonNode Content,
    JsonNode? Grading = null,
    List<string>? SkillTags = null
);

public sealed record ReorderExercisesRequest(
    List<Guid> OrderedExerciseIds
);

public sealed record PublishLessonRequest(
    string? RowVersion
);

public sealed record PublishedLessonResultDto(
    Guid LessonId,
    int Version,
    string Status,
    DateTimeOffset PublishedAt,
    string? RowVersion,
    int ExerciseCount,
    decimal TotalMaxPoints
);

public sealed record DraftRevisionResultDto(
    Guid DraftLessonId,
    Guid PublishedParentLessonId,
    int Version,
    string Status,
    string? RowVersion
);

public sealed record CommitLessonImportRequest(
    Guid? TargetLessonId,
    int? TargetRevision,
    string CommitMode, // Append | Replace
    JsonNode Candidate
);

public sealed record ValidationIssueDto(
    string Path,
    string Severity, // Error | Warning
    string Message
);

public sealed record LessonCandidateValidationResultDto(
    bool IsValid,
    List<ValidationIssueDto> Issues,
    JsonNode? Candidate
);

public sealed record LessonPromptResultDto(
    string SystemPrompt,
    string UserPrompt,
    string PromptText,
    string SchemaVersion
);
