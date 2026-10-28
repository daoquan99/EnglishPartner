using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Conversations.Commands.CreateConversation;

public sealed class CreateConversationValidator : AbstractValidator<CreateConversationCommand>
{
    public CreateConversationValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");
    }
}
