using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Conversations.Queries.PreviewSystemPrompt;

public sealed class PreviewSystemPromptValidator : AbstractValidator<PreviewSystemPromptQuery>
{
    public PreviewSystemPromptValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");
    }
}
