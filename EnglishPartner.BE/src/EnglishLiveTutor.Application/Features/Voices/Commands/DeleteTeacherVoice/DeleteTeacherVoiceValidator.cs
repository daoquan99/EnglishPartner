using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Voices.Commands.DeleteTeacherVoice;

public sealed class DeleteTeacherVoiceValidator : AbstractValidator<DeleteTeacherVoiceCommand>
{
    public DeleteTeacherVoiceValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
    }
}
