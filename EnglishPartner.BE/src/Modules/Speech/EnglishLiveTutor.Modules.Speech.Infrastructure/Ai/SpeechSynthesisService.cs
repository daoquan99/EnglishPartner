using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Modules.Speech.Contracts;

namespace EnglishLiveTutor.Modules.Speech.Infrastructure.Ai;

public sealed class SpeechSynthesisService : ISpeechSynthesisService
{
    private readonly ILivePronunciationTtsSynthesizer _tts;

    public SpeechSynthesisService(ILivePronunciationTtsSynthesizer tts)
    {
        _tts = tts ?? throw new ArgumentNullException(nameof(tts));
    }

    public async Task<SpeechSynthesisResultDto> SynthesizeAsync(
        string text,
        string? voice = null,
        string? accent = null,
        double speed = 1.0,
        string? apiKey = null,
        string? modelCode = null,
        CancellationToken ct = default)
    {
        var request = new SynthesizePronunciationAudioRequest(
            ApiKey: apiKey ?? string.Empty,
            Text: text,
            TargetType: "sentence",
            Accent: string.IsNullOrWhiteSpace(accent) ? "US" : accent,
            VoiceName: string.IsNullOrWhiteSpace(voice) ? "Aoede" : voice,
            SpeechSpeed: speed <= 0 ? 1.0 : speed,
            ModelCode: string.IsNullOrWhiteSpace(modelCode) ? "gemini-3.8-live" : modelCode
        );

        var result = await _tts.SynthesizeSpeechAsync(request, ct);
        return new SpeechSynthesisResultDto(result.MimeType, result.AudioBase64, result.DurationMs);
    }
}
