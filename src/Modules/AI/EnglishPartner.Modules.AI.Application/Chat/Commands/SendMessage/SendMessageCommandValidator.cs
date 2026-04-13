using FluentValidation;

namespace EnglishPartner.Modules.AI.Application.Chat.Commands.SendMessage;

internal sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Message cannot be empty.")
            .MaximumLength(4000);
    }
}
