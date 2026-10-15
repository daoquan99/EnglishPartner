using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Learning.Queries.GetDailyVocabularyPlan;

public sealed class GetDailyVocabularyPlanValidator : AbstractValidator<GetDailyVocabularyPlanQuery>
{
    public GetDailyVocabularyPlanValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
    }
}
