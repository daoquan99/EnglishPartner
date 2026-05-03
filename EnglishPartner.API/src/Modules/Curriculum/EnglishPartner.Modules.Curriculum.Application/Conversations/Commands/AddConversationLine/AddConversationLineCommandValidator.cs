using FluentValidation;

namespace EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.AddConversationLine;

internal sealed class AddConversationLineCommandValidator : AbstractValidator<AddConversationLineCommand>
{
    public AddConversationLineCommandValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty();

        RuleFor(x => x.Speaker)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("Speaker name is required and must be ≤ 100 characters.");

        RuleFor(x => x.Text)
            .NotEmpty()
            .MaximumLength(2000)
            .WithMessage("Line text is required and must be ≤ 2000 characters.");

        RuleForEach(x => x.Translations)
            .ChildRules(entry =>
            {
                entry.RuleFor(e => e.Key).NotEmpty().MaximumLength(10);
                entry.RuleFor(e => e.Value).NotEmpty().MaximumLength(2000);
            })
            .When(x => x.Translations is not null);

        RuleFor(x => x.AudioUrl)
            .MaximumLength(500)
            .When(x => x.AudioUrl is not null);
    }
}
