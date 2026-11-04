using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetPronunciationAudio;

public sealed class GetPronunciationAudioValidator : AbstractValidator<GetPronunciationAudioQuery>
{
    public GetPronunciationAudioValidator()
    {
        RuleFor(x => x.TargetId).NotEmpty().WithMessage("TargetId không được để trống.");
    }
}
