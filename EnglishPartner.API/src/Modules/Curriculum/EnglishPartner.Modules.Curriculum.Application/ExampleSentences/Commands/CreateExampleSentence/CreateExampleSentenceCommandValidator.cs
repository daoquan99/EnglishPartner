using FluentValidation;

namespace EnglishPartner.Modules.Curriculum.Application.ExampleSentences.Commands.CreateExampleSentence;

public sealed class CreateExampleSentenceCommandValidator : AbstractValidator<CreateExampleSentenceCommand>
{
    public CreateExampleSentenceCommandValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.LanguageCode).NotEmpty().MaximumLength(10);
        RuleForEach(x => x.Translations)
            .ChildRules(entry =>
            {
                entry.RuleFor(e => e.Key).NotEmpty().MaximumLength(10);
                entry.RuleFor(e => e.Value).NotEmpty().MaximumLength(2000);
            })
            .When(x => x.Translations is not null);
        RuleFor(x => x.AudioUrl).MaximumLength(500);
        RuleFor(x => x.Note).MaximumLength(1000);
        RuleFor(x => x.Level).MaximumLength(20);
    }
}
