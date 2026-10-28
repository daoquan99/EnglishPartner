using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Prompts.Queries.PreviewSystemPrompt;

public sealed class PreviewSystemPromptValidator : AbstractValidator<PreviewSystemPromptQuery>
{
    public PreviewSystemPromptValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");
    }
}
