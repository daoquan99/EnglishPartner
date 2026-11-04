using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Conversations.Ports;
using EnglishLiveTutor.Modules.Speech.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishLiveTutor.Api.Services;

public sealed class LiveFeedbackAudioSynthesizer : IFeedbackAudioSynthesizer
{
    private readonly IServiceScopeFactory _scopeFactory;

    public LiveFeedbackAudioSynthesizer(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    public async Task<FeedbackAudioResultDto> SynthesizeFeedbackAudioAsync(
        string apiKey,
        string text,
        string voiceName,
        double speechSpeed,
        string accent,
        string? modelCode = null,
        CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var speechService = scope.ServiceProvider.GetRequiredService<ISpeechSynthesisService>();

        var result = await speechService.SynthesizeAsync(
            text: text,
            voice: voiceName,
            accent: accent,
            speed: speechSpeed,
            apiKey: apiKey,
            modelCode: modelCode,
            ct: ct);

        return new FeedbackAudioResultDto(result.MimeType, result.AudioBase64, result.DurationMs);
    }
}
