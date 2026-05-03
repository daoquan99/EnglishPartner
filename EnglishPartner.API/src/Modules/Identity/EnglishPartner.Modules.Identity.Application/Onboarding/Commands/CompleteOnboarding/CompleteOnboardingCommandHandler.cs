using EnglishPartner.Modules.Identity.Domain;
using EnglishPartner.Modules.Identity.Domain.Users.Entities;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Identity.Application.Onboarding.Commands.CompleteOnboarding;

internal sealed class CompleteOnboardingCommandHandler(
    IUserRepository userRepository,
    IUserProfileRepository profileRepository,
    IOnboardingAnswerRepository onboardingAnswerRepository) : ICommandHandler<CompleteOnboardingCommand>
{
    public async Task<Result> Handle(CompleteOnboardingCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(IdentityErrors.User.NotFoundById(request.UserId));

        if (user.IsOnboardingCompleted)
            return Result.Failure(IdentityErrors.User.OnboardingAlreadyCompleted);

        var profile = await profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        var isNewProfile = profile is null;

        if (isNewProfile)
            profile = UserProfile.Create(request.UserId);

        // Update profile preferences.
        profile!.Update(
            nativeLanguage: request.NativeLanguage,
            targetLanguage: request.TargetLanguage,
            proficiencyLevel: request.ProficiencyLevel,
            learningGoal: request.LearningGoal,
            dailyGoalMinutes: request.DailyGoalMinutes,
            avatarUrl: null,
            dateOfBirth: null,
            morningReminderHour: request.MorningReminderHour,
            eveningReminderHour: request.EveningReminderHour);

        // Save onboarding answers for audit/analytics.
        var answers = new List<OnboardingAnswer>
        {
            OnboardingAnswer.Create(request.UserId, "native_language",       request.NativeLanguage,                    1),
            OnboardingAnswer.Create(request.UserId, "target_language",       request.TargetLanguage,                    2),
            OnboardingAnswer.Create(request.UserId, "proficiency_level",     request.ProficiencyLevel,                  3),
            OnboardingAnswer.Create(request.UserId, "learning_goal",        request.LearningGoal,                      4),
            OnboardingAnswer.Create(request.UserId, "daily_goal",           request.DailyGoalMinutes.ToString(),       5),
        };

        if (request.MorningReminderHour.HasValue)
            answers.Add(OnboardingAnswer.Create(request.UserId, "morning_reminder_hour", request.MorningReminderHour.Value.ToString(), 6));

        if (request.EveningReminderHour.HasValue)
            answers.Add(OnboardingAnswer.Create(request.UserId, "evening_reminder_hour", request.EveningReminderHour.Value.ToString(), 7));

        onboardingAnswerRepository.AddRange(answers);

        // Read UtcOffsetHours already captured at Register time (from X-Timezone-Offset header).
        user.CompleteOnboarding(
            request.ProficiencyLevel,
            request.LearningGoal,
            request.TargetLanguage,
            request.DailyGoalMinutes,
            request.MorningReminderHour,
            request.EveningReminderHour,
            profile.UtcOffsetHours);

        if (isNewProfile)
            profileRepository.Add(profile);
        else
            profileRepository.Update(profile);

        return Result.Success();
    }
}
