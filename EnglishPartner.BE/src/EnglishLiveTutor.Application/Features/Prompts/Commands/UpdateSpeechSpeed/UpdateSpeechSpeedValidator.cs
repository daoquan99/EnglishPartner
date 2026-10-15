using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Prompts.Commands.UpdateSpeechSpeed;

public sealed class UpdateSpeechSpeedValidator : AbstractValidator<UpdateSpeechSpeedCommand>
{
    public UpdateSpeechSpeedValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
        RuleFor(x => x.Request).NotNull().WithMessage("Request khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
    }
}
