using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Conversations.Commands.DeleteConversation;

public sealed class DeleteConversationValidator : AbstractValidator<DeleteConversationCommand>
{
    public DeleteConversationValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id không được để trống.");
    }
}
