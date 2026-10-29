using System.Text.Json;
using System.Text.Json.Serialization;
using EnglishLiveTutor.Domain.Grammar.Enums;
using EnglishLiveTutor.Domain.Lessons.Enums;

namespace EnglishLiveTutor.Application.Features.Grammar.Models;

public sealed record GrammarStepAdminDto(
    Guid Id,
    string Code,
    string CefrLevel,
    int Order,
    string TitleVi,
    string TitleEn,
    string SummaryVi,
    bool IsLevelReview,
    bool IsPublished,
    bool TheoryEditedByAdmin,
    int TheoryVersion,
    int CoreExerciseCount,
    int OfficialExerciseCount,
    int PendingExerciseCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc
);

public sealed record GrammarStepAdminDetailDto(
    Guid Id,
    string Code,
    string CefrLevel,
    int Order,
    string TitleVi,
    string TitleEn,
    string SummaryVi,
    bool IsLevelReview,
    bool IsPublished,
    bool TheoryEditedByAdmin,
    int TheoryVersion,
    JsonElement Theory,
    IReadOnlyList<GrammarExerciseAdminDto> OfficialExercises,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc
);

public sealed record GrammarExerciseAdminDto(
    Guid Id,
    Guid StepId,
    string? Key,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] ExerciseType Type,
    int Difficulty,
    string Title,
    string Instructions,
    string? Focus,
    JsonElement Content,
    JsonElement? Grading,
    decimal MaxPoints,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] GradingMode GradingMode,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] GrammarExerciseSource Source,
    string? OwnerUserId,
    string? AuthorUserId,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] GrammarExerciseVisibility Visibility,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] GrammarModerationStatus Moderation,
    string? RejectReason,
    string? ReviewedBy,
    DateTimeOffset? ReviewedAtUtc,
    bool IsCore,
    string ContentHash,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc
);

public sealed record UpdateGrammarStepRequest(
    string TitleVi,
    string TitleEn,
    string SummaryVi,
    bool IsPublished
);

public sealed record UpdateGrammarStepTheoryRequest(
    JsonElement Theory
);

public sealed record CreateOfficialGrammarExerciseRequest(
    string? Key,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] ExerciseType Type,
    int Difficulty,
    string Title,
    string Instructions,
    decimal MaxPoints,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] GradingMode GradingMode,
    JsonElement Content,
    JsonElement? Grading,
    bool IsCore,
    string? Focus = null
);

public sealed record UpdateOfficialGrammarExerciseRequest(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] ExerciseType Type,
    int Difficulty,
    string Title,
    string Instructions,
    decimal MaxPoints,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] GradingMode GradingMode,
    JsonElement Content,
    JsonElement? Grading,
    bool IsCore,
    string? Focus = null
);

public sealed record ImportGrammarContentPackRequest(
    bool OverwriteExistingTheory = false
);
