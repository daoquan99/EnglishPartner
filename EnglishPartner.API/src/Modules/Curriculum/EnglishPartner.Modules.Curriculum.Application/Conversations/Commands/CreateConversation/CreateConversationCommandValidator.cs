using FluentValidation;

namespace EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.CreateConversation;

public sealed class CreateConversationCommandValidator : AbstractValidator<CreateConversationCommand>
{
    public CreateConversationCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.LanguageCode).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Scenario).MaximumLength(1000);
        RuleFor(x => x.Level).MaximumLength(20);
    }
}
