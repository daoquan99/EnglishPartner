using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Commands.UpdateVocabulary;

public sealed class UpdateVocabularyValidator : AbstractValidator<UpdateVocabularyCommand>
{
    public UpdateVocabularyValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
        RuleFor(x => x.Request).NotNull().WithMessage("Request khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
    }
}
