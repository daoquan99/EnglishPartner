using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Prompts.Commands.UpdateSpeechSpeed;

public sealed class UpdateSpeechSpeedValidator : AbstractValidator<UpdateSpeechSpeedCommand>
{
    public UpdateSpeechSpeedValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id không được để trống.");
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");
    }
}
