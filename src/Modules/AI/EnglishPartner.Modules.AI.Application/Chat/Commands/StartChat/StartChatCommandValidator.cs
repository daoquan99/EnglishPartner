using FluentValidation;

namespace EnglishPartner.Modules.AI.Application.Chat.Commands.StartChat;

internal sealed class StartChatCommandValidator : AbstractValidator<StartChatCommand>
{
    public StartChatCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Chat session title is required.")
            .MaximumLength(200);
        RuleFor(x => x.SystemPrompt)
            .MaximumLength(4000)
            .When(x => x.SystemPrompt is not null);
    }
}
