using FluentValidation;

namespace EnglishLiveTutor.Application.Features.AiModels.Queries.GetVocabularyGenerationModels;

public sealed class GetVocabularyGenerationModelsValidator : AbstractValidator<GetVocabularyGenerationModelsQuery>
{
    public GetVocabularyGenerationModelsValidator()
    {
        // Validation rules can be configured here
    }
}
