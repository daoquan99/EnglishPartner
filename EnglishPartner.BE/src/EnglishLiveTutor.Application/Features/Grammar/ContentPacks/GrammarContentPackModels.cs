using System.Text.Json;
using System.Text.Json.Serialization;

namespace EnglishLiveTutor.Application.Features.Grammar.ContentPacks;

public sealed record GrammarContentPackDto(
    [property: JsonPropertyName("packVersion")] int PackVersion,
    [property: JsonPropertyName("level")] string Level,
    [property: JsonPropertyName("steps")] List<GrammarContentPackStepDto>? Steps
);

public sealed record GrammarContentPackStepDto(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("order")] int Order,
    [property: JsonPropertyName("titleVi")] string TitleVi,
    [property: JsonPropertyName("titleEn")] string TitleEn,
    [property: JsonPropertyName("summaryVi")] string SummaryVi,
    [property: JsonPropertyName("isLevelReview")] bool IsLevelReview,
    [property: JsonPropertyName("theoryVersion")] int TheoryVersion,
    [property: JsonPropertyName("theory")] JsonElement Theory,
    [property: JsonPropertyName("exercises")] List<GrammarContentPackExerciseDto>? Exercises
);

public sealed record GrammarContentPackExerciseDto(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("difficulty")] int Difficulty,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("instructions")] string Instructions,
    [property: JsonPropertyName("maxPoints")] decimal MaxPoints,
    [property: JsonPropertyName("gradingMode")] string GradingMode,
    [property: JsonPropertyName("isCore")] bool IsCore,
    [property: JsonPropertyName("content")] JsonElement Content,
    [property: JsonPropertyName("grading")] JsonElement? Grading,
    [property: JsonPropertyName("focus")] string? Focus = null
);

public sealed record GrammarImportIssue(
    string? Code,
    string? Key,
    string Message
);

public sealed record GrammarContentPackImportResult(
    int StepsCreated,
    int StepsUpdated,
    int StepsSkipped,
    int ExercisesCreated,
    int ExercisesSkipped,
    IReadOnlyList<GrammarImportIssue> Errors
);
