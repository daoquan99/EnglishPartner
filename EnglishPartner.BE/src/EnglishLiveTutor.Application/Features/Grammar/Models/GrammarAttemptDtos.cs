using System.Text.Json;
using System.Text.Json.Serialization;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Grammar.Enums;

namespace EnglishLiveTutor.Application.Features.Grammar.Models;

public sealed record StartGrammarAttemptRequest(
    [property: JsonConverter(typeof(JsonStringEnumConverter))] GrammarAttemptKind Kind,
    string? StepCode = null,
    string? CefrLevel = null,
    bool ForceNew = false
);

public sealed record SubmitGrammarAnswerRequest(
    Guid ExerciseId,
    JsonElement AnswerPayload,
    LessonAiExecutionConfigDto? AiConfig = null
);

public sealed record GrammarAttemptDto(
    Guid Id,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] GrammarAttemptKind Kind,
    string? StepCode,
    string CefrLevel,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    decimal? Score100,
    IReadOnlyList<GrammarExerciseLearnerDto> Exercises,
    IReadOnlyList<GrammarAttemptAnswerDto> Answers
);

public sealed record GrammarAttemptAnswerDto(
    Guid ExerciseId,
    string Status,
    decimal? Score100,
    decimal? PointsEarned,
    decimal MaxPoints,
    JsonElement? Feedback,
    JsonElement? AnswerPayload
);

public sealed record GrammarAnswerResultDto(
    Guid AttemptId,
    Guid ExerciseId,
    string Status,
    decimal? Score100,
    decimal? PointsEarned,
    decimal MaxPoints,
    JsonElement? Feedback,
    JsonElement? AnswerPayload,
    string? SuggestedAnswer = null
);

public sealed record GrammarAttemptResultDto(
    decimal Score100,
    bool Passed,
    IReadOnlyList<string> UnlockedStepCodes,
    bool? LevelPassed = null
);
