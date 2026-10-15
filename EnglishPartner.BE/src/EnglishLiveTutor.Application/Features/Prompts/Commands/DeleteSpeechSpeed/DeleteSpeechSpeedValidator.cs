using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Prompts.Commands.DeleteSpeechSpeed;

public sealed class DeleteSpeechSpeedValidator : AbstractValidator<DeleteSpeechSpeedCommand>
{
    public DeleteSpeechSpeedValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
    }
}
