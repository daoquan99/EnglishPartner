using FluentValidation;

namespace EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.ReorderConversationLines;

internal sealed class ReorderConversationLinesCommandValidator : AbstractValidator<ReorderConversationLinesCommand>
{
    public ReorderConversationLinesCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.OrderedLineIds).NotNull();
        RuleForEach(x => x.OrderedLineIds).NotEmpty();
    }
}
