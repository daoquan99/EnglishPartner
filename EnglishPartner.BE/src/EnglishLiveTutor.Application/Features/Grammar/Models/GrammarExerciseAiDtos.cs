using System.Text.Json;
using System.Text.Json.Serialization;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Grammar.Enums;
using EnglishLiveTutor.Domain.Lessons.Enums;

namespace EnglishLiveTutor.Application.Features.Grammar.Models;

public sealed record GrammarAiExecutionConfig(
    string? Provider = null,
    string? ModelCode = null,
    string? ApiKey = null
);

public sealed record GrammarExercisePromptRequestedItem(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] ExerciseType Type,
    int Count,
    int Difficulty
);

public sealed record GrammarExercisePromptInput(
    string StepCode,
    string CefrLevel,
    string TitleEn,
    string TitleVi,
    IReadOnlyList<string> Formulas,
    IReadOnlyList<string> CommonMistakes,
    IReadOnlyList<string> PriorSteps,
    IReadOnlyList<string> LaterTopicsToAvoid,
    IReadOnlyList<GrammarExercisePromptRequestedItem> RequestedItems,
    string? LearnerNote = null,
    bool IsLevelReview = false
);

public sealed record GenerateGrammarExercisesRequest(
    IReadOnlyList<GrammarExercisePromptRequestedItem> Items,
    string? LearnerNote = null,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] GrammarExerciseVisibility Visibility = GrammarExerciseVisibility.Private,
    GrammarAiExecutionConfig? AiConfig = null
);

public sealed record BuildGrammarExercisePromptRequest(
    IReadOnlyList<GrammarExercisePromptRequestedItem> Items,
    string? LearnerNote = null
);

public sealed record BuildGrammarExercisePromptResponse(string Prompt);

public sealed record ImportGrammarExercisesJsonRequest(
    string JsonText,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] GrammarExerciseVisibility Visibility = GrammarExerciseVisibility.Private,
    IReadOnlyList<GrammarExercisePromptRequestedItem>? ExpectedItems = null
);

public sealed record GrammarExerciseRejectionDto(
    int Index,
    IReadOnlyList<string> Issues
);

public sealed record GrammarMyExerciseDto(
    Guid Id,
    Guid StepId,
    string StepCode,
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

public sealed record GenerateGrammarExercisesResponse(
    IReadOnlyList<GrammarMyExerciseDto> Created,
    IReadOnlyList<GrammarExerciseRejectionDto> Rejected
);

public sealed record UpdateMyGrammarExerciseRequest(
    string Title,
    string Instructions,
    int Difficulty,
    JsonElement Content,
    JsonElement? Grading,
    string? Focus = null
);

public sealed record ChangeMyGrammarExerciseVisibilityRequest(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] GrammarExerciseVisibility Visibility
);

public sealed record GrammarModerationItemDto(
    Guid Id,
    Guid StepId,
    string StepCode,
    string CefrLevel,
    string? AuthorUserId,
    string? AuthorName,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] ExerciseType Type,
    int Difficulty,
    string Title,
    string Instructions,
    string? Focus,
    JsonElement Content,
    JsonElement? Grading,
    decimal MaxPoints,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] GradingMode GradingMode,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] GrammarExerciseVisibility Visibility,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] GrammarModerationStatus Moderation,
    string? RejectReason,
    DateTimeOffset CreatedAtUtc
);

public sealed record RejectGrammarExerciseRequest(string Reason);

public sealed record PromoteGrammarExerciseRequest(bool IsCore);

public sealed record GenerateOfficialGrammarExercisesRequest(
    IReadOnlyList<GrammarExercisePromptRequestedItem> Items,
    bool IsCore = false,
    GrammarAiExecutionConfig? AiConfig = null
);

public sealed record ImportOfficialGrammarExercisesJsonRequest(
    string JsonText,
    bool IsCore = false,
    IReadOnlyList<GrammarExercisePromptRequestedItem>? ExpectedItems = null
);

public sealed record DraftGrammarTheoryWithAiRequest(GrammarAiExecutionConfig? AiConfig = null);

public sealed record DraftGrammarTheoryWithAiResponse(
    JsonElement Theory,
    IReadOnlyList<ValidationIssueDto> Issues
);
