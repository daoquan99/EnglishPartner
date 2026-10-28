using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Commands.GenerateVocabulary;

public sealed class GenerateVocabularyValidator : AbstractValidator<GenerateVocabularyCommand>
{
    public GenerateVocabularyValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId không được để trống.");
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");
    }
}
