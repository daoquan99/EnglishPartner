using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Conversations.Commands.UpdateConversationSettings;

public sealed class UpdateConversationSettingsValidator : AbstractValidator<UpdateConversationSettingsCommand>
{
    public UpdateConversationSettingsValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Id không được để trống.");
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");
    }
}
