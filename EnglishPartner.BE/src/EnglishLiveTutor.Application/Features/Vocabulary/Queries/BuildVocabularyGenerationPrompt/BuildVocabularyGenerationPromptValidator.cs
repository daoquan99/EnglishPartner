using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Queries.BuildVocabularyGenerationPrompt;

public sealed class BuildVocabularyGenerationPromptValidator : AbstractValidator<BuildVocabularyGenerationPromptQuery>
{
    public BuildVocabularyGenerationPromptValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Request khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
    }
}
