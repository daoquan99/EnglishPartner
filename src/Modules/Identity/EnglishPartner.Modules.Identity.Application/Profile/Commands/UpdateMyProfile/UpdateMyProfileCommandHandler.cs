using EnglishPartner.Modules.Identity.Domain;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Identity.Application.Profile.Commands.UpdateMyProfile;

internal sealed class UpdateMyProfileCommandHandler(
    IUserRepository userRepository,
    IUserProfileRepository profileRepository) : ICommandHandler<UpdateMyProfileCommand>
{
    public async Task<Result> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure(IdentityErrors.User.NotFoundById(request.UserId));

        if (request.DisplayName is not null)
            user.UpdateProfile(request.DisplayName);

        var profile = await profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile is null)
            return Result.Failure(IdentityErrors.User.NotFoundById(request.UserId));

        profile.Update(
            nativeLanguage: request.NativeLanguage,
            targetLanguage: request.TargetLanguage,
            proficiencyLevel: request.ProficiencyLevel,
            learningGoal: request.LearningGoal,
            dailyGoalMinutes: request.DailyGoalMinutes,
            avatarUrl: request.AvatarUrl,
            dateOfBirth: request.DateOfBirth);

        userRepository.Update(user);
        profileRepository.Update(profile);

        return Result.Success();
    }
}
