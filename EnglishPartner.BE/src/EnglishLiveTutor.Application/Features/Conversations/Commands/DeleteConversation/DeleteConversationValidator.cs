using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Conversations.Commands.DeleteConversation;

public sealed class DeleteConversationValidator : AbstractValidator<DeleteConversationCommand>
{
    public DeleteConversationValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
    }
}
