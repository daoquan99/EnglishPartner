using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Learning.Queries.GetDailyVocabularyPlan;

public sealed class GetDailyVocabularyPlanValidator : AbstractValidator<GetDailyVocabularyPlanQuery>
{
    public GetDailyVocabularyPlanValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId không được để trống.");
    }
}
