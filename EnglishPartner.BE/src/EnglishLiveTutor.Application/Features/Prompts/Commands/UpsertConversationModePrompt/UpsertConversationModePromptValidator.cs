using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Prompts.Commands.UpsertConversationModePrompt;

public sealed class UpsertConversationModePromptValidator : AbstractValidator<UpsertConversationModePromptCommand>
{
    public UpsertConversationModePromptValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");
    }
}
