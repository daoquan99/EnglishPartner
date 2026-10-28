using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Commands.AssessPronunciation;

public sealed class AssessPronunciationValidator : AbstractValidator<AssessPronunciationCommand>
{
    public AssessPronunciationValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId không được để trống.");
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");
    }
}
