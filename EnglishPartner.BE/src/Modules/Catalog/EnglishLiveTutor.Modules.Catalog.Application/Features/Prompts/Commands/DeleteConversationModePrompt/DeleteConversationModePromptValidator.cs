using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Prompts.Commands.DeleteConversationModePrompt;

public sealed class DeleteConversationModePromptValidator : AbstractValidator<DeleteConversationModePromptCommand>
{
    public DeleteConversationModePromptValidator()
    {
        // Validation rules can be configured here
    }
}
