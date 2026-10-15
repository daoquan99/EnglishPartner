using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Commands.SynthesizePronunciationAudio;

public sealed record SynthesizePronunciationAudioCommand(SynthesizePronunciationAudioRequest Request)
    : ICommand<SynthesizePronunciationAudioResponseDto>;

public sealed class SynthesizePronunciationAudioCommandHandler(ILivePronunciationTtsSynthesizer synthesizer)
    : ICommandHandler<SynthesizePronunciationAudioCommand, SynthesizePronunciationAudioResponseDto>
{
    public Task<SynthesizePronunciationAudioResponseDto> HandleAsync(SynthesizePronunciationAudioCommand command, CancellationToken ct = default) =>
        synthesizer.SynthesizeSpeechAsync(command.Request, ct);
}
