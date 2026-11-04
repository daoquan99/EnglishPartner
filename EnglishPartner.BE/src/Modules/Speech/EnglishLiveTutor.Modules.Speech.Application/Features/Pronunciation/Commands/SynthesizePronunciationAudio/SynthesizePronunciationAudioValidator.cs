using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Commands.SynthesizePronunciationAudio;

public sealed class SynthesizePronunciationAudioValidator : AbstractValidator<SynthesizePronunciationAudioCommand>
{
    public SynthesizePronunciationAudioValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");
    }
}
