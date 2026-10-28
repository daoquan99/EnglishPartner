using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Commands.UpdateVocabulary;

public sealed class UpdateVocabularyValidator : AbstractValidator<UpdateVocabularyCommand>
{
    public UpdateVocabularyValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id không được để trống.");
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");
    }
}
