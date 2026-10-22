namespace EnglishLiveTutor.Application.Common.Interfaces;

public interface ILessonAiGenerator
{
    Task<string> GenerateLessonExercisesJsonAsync(
        string promptText,
        string? clientApiKey,
        string? modelCode,
        CancellationToken ct = default);
}
