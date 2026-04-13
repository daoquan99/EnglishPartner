using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Identity.Application.Onboarding.Commands.CompleteOnboarding;

public sealed record CompleteOnboardingCommand(
    Guid UserId,
    string NativeLanguage,
    string TargetLanguage,
    string ProficiencyLevel,
    string LearningGoal,
    int DailyGoalMinutes) : ICommand;
