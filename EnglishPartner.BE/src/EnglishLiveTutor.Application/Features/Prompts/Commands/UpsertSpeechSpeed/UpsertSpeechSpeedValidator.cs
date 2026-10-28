using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Prompts.Commands.UpsertSpeechSpeed;

public sealed class UpsertSpeechSpeedValidator : AbstractValidator<UpsertSpeechSpeedCommand>
{
    public UpsertSpeechSpeedValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");
    }
}
