using EnglishPartner.Modules.Identity.Domain;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Identity.Application.Onboarding.Commands.CompleteOnboarding;

internal sealed class CompleteOnboardingCommandHandler(
    IUserRepository userRepository,
    IUserProfileRepository profileRepository) : ICommandHandler<CompleteOnboardingCommand>
{
    public async Task<Result> Handle(CompleteOnboardingCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(IdentityErrors.User.NotFoundById(request.UserId));

        if (user.IsOnboardingCompleted)
            return Result.Failure(IdentityErrors.User.OnboardingAlreadyCompleted);

        var profile = await profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile is null)
            return Result.Failure(IdentityErrors.User.NotFoundById(request.UserId));

        profile.Update(
            nativeLanguage: request.NativeLanguage,
            targetLanguage: request.TargetLanguage,
            proficiencyLevel: request.ProficiencyLevel,
            learningGoal: request.LearningGoal,
            dailyGoalMinutes: request.DailyGoalMinutes,
            avatarUrl: null,
            dateOfBirth: null);

        user.CompleteOnboarding();
        userRepository.Update(user);
        profileRepository.Update(profile);

        return Result.Success();
    }
}
