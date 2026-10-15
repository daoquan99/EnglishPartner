using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetPronunciationAudio;

public sealed class GetPronunciationAudioValidator : AbstractValidator<GetPronunciationAudioQuery>
{
    public GetPronunciationAudioValidator()
    {
        RuleFor(x => x.TargetId).NotEmpty().WithMessage("TargetId khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
    }
}
