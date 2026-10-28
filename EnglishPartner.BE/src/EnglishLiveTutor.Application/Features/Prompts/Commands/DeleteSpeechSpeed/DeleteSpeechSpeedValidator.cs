using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Prompts.Commands.DeleteSpeechSpeed;

public sealed class DeleteSpeechSpeedValidator : AbstractValidator<DeleteSpeechSpeedCommand>
{
    public DeleteSpeechSpeedValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id không được để trống.");
    }
}
