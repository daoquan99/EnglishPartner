using EnglishLiveTutor.Application.Common.Models;

namespace EnglishLiveTutor.Application.Common.Interfaces;

public sealed record LessonPromptInput(
    string Topic,
    string CefrLevel,
    string? LessonTitle = null,
    IReadOnlyList<string>? PreferredExerciseTypes = null,
    int TargetExerciseCount = 5,
    string? CustomInstructions = null);

public interface ILessonPromptBuilder
{
    Task<LessonPromptResultDto> BuildPromptAsync(LessonPromptInput input, CancellationToken ct = default);
}
