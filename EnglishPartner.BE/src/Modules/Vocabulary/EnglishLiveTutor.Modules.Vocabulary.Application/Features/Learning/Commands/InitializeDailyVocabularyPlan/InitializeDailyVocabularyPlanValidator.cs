using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Learning.Commands.InitializeDailyVocabularyPlan;

public sealed class InitializeDailyVocabularyPlanValidator : AbstractValidator<InitializeDailyVocabularyPlanCommand>
{
    public InitializeDailyVocabularyPlanValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId không được để trống.");
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");
    }
}
