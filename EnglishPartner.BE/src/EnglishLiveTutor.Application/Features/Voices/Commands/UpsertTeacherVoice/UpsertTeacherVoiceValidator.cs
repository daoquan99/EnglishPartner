using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Voices.Commands.UpsertTeacherVoice;

public sealed class UpsertTeacherVoiceValidator : AbstractValidator<UpsertTeacherVoiceCommand>
{
    public UpsertTeacherVoiceValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id không được để trống.");
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");
    }
}
