using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Commands.AssessPronunciation;

public sealed class AssessPronunciationValidator : AbstractValidator<AssessPronunciationCommand>
{
    public AssessPronunciationValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
        RuleFor(x => x.Request).NotNull().WithMessage("Request khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
    }
}
