using FluentValidation;

namespace EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.UpdateConversationLine;

internal sealed class UpdateConversationLineCommandValidator : AbstractValidator<UpdateConversationLineCommand>
{
    public UpdateConversationLineCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.LineId).NotEmpty();
        RuleFor(x => x.Speaker).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Text).NotEmpty().MaximumLength(1000);
    }
}
