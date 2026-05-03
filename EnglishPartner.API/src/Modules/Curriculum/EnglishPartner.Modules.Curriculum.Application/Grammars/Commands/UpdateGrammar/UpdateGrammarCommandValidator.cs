using FluentValidation;

namespace EnglishPartner.Modules.Curriculum.Application.Grammars.Commands.UpdateGrammar;

public sealed class UpdateGrammarCommandValidator : AbstractValidator<UpdateGrammarCommand>
{
    public UpdateGrammarCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Level).MaximumLength(20);
        RuleFor(x => x.Explanation).MaximumLength(5000);
        RuleFor(x => x.Formula).MaximumLength(500);
    }
}
