namespace EnglishLiveTutor.Application.Common.Interfaces;

public interface ILessonAiGenerator
{
    Task<string> GenerateLessonExercisesJsonAsync(
        string promptText,
        string? clientApiKey,
        string? modelCode,
        string? provider = null,
        CancellationToken ct = default);
}

