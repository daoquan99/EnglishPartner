using System.Text.Json;
using System.Text.Json.Serialization;
using EnglishLiveTutor.Domain.Grammar.Enums;
using EnglishLiveTutor.Domain.Lessons.Enums;

namespace EnglishLiveTutor.Application.Features.Grammar.Models;

public sealed record GrammarExerciseLearnerDto(
    Guid Id,
    string? Key,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] ExerciseType Type,
    string? Title,
    string? Instructions,
    int Order,
    decimal MaxPoints,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] GradingMode GradingMode,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] JsonElement? Content = null,
    int Difficulty = 0,
    string? Focus = null,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] GrammarExerciseSource Source = default,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] GrammarExerciseVisibility Visibility = default,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] GrammarModerationStatus Moderation = default,
    string? AuthorName = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool Unavailable = false
);
