using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Prompts.Queries.PreviewSystemPrompt;

public sealed class PreviewSystemPromptValidator : AbstractValidator<PreviewSystemPromptQuery>
{
    public PreviewSystemPromptValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Request khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
    }
}
