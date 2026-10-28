using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Commands.ImportVocabularyJson;

public sealed class ImportVocabularyJsonValidator : AbstractValidator<ImportVocabularyJsonCommand>
{
    public ImportVocabularyJsonValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId không được để trống.");
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");
    }
}
