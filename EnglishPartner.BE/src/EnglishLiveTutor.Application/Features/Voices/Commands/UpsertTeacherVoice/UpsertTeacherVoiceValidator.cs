using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Voices.Commands.UpsertTeacherVoice;

public sealed class UpsertTeacherVoiceValidator : AbstractValidator<UpsertTeacherVoiceCommand>
{
    public UpsertTeacherVoiceValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
        RuleFor(x => x.Request).NotNull().WithMessage("Request khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
    }
}
