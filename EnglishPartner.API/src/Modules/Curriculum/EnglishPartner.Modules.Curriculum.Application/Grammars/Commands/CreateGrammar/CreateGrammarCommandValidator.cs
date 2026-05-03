using FluentValidation;

namespace EnglishPartner.Modules.Curriculum.Application.Grammars.Commands.CreateGrammar;

public sealed class CreateGrammarCommandValidator : AbstractValidator<CreateGrammarCommand>
{
    public CreateGrammarCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LanguageCode).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Level).MaximumLength(20);
        RuleFor(x => x.Explanation).MaximumLength(5000);
        RuleFor(x => x.Formula).MaximumLength(500);
    }
}
