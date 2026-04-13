using FluentValidation;

namespace EnglishPartner.Modules.Identity.Application.Onboarding.Commands.CompleteOnboarding;

public sealed class CompleteOnboardingCommandValidator : AbstractValidator<CompleteOnboardingCommand>
{
    public CompleteOnboardingCommandValidator()
    {
        RuleFor(x => x.NativeLanguage).NotEmpty().MaximumLength(10);
        RuleFor(x => x.TargetLanguage).NotEmpty().MaximumLength(10);
        RuleFor(x => x.ProficiencyLevel).NotEmpty().MaximumLength(20);
        RuleFor(x => x.LearningGoal).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DailyGoalMinutes).InclusiveBetween(5, 120);
    }
}
