using EnglishLiveTutor.Application.Common.Models;

namespace EnglishLiveTutor.Application.Features.Conversations.Ports;

public interface IFeedbackAudioSynthesizer
{
    Task<FeedbackAudioResultDto> SynthesizeFeedbackAudioAsync(
        string apiKey,
        string text,
        string voiceName,
        double speechSpeed,
        string accent,
        string? modelCode = null,
        CancellationToken ct = default);
}
