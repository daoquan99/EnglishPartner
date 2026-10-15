using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Commands.ImportVocabularyJson;

public sealed class ImportVocabularyJsonValidator : AbstractValidator<ImportVocabularyJsonCommand>
{
    public ImportVocabularyJsonValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
        RuleFor(x => x.Request).NotNull().WithMessage("Request khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
    }
}
