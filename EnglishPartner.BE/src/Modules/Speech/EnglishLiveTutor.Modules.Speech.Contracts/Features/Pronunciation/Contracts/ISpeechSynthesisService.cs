namespace EnglishLiveTutor.Modules.Speech.Contracts;

public sealed record SpeechSynthesisResultDto(
    string MimeType,
    string AudioBase64,
    int DurationMs
);

public interface ISpeechSynthesisService
{
    Task<SpeechSynthesisResultDto> SynthesizeAsync(
        string text,
        string? voice = null,
        string? accent = null,
        double speed = 1.0,
        string? apiKey = null,
        string? modelCode = null,
        CancellationToken ct = default);
}
