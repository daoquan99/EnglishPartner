using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Conversations.Commands.UpdateConversationSettings;

public sealed class UpdateConversationSettingsValidator : AbstractValidator<UpdateConversationSettingsCommand>
{
    public UpdateConversationSettingsValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
        RuleFor(x => x.Request).NotNull().WithMessage("Request khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
    }
}
