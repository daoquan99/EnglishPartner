using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Ports;

public interface ILivePronunciationTtsSynthesizer
{
    Task<SynthesizePronunciationAudioResponseDto> SynthesizeSpeechAsync(
        SynthesizePronunciationAudioRequest request,
        CancellationToken ct = default);
}
