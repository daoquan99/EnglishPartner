using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Voices.Commands.DeleteTeacherVoice;

public sealed class DeleteTeacherVoiceValidator : AbstractValidator<DeleteTeacherVoiceCommand>
{
    public DeleteTeacherVoiceValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id không được để trống.");
    }
}
