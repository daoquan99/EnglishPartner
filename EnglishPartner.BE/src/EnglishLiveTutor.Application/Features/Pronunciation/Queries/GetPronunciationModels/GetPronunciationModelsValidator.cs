using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetPronunciationModels;

public sealed class GetPronunciationModelsValidator : AbstractValidator<GetPronunciationModelsQuery>
{
    public GetPronunciationModelsValidator()
    {
        // Validation rules can be configured here
    }
}
