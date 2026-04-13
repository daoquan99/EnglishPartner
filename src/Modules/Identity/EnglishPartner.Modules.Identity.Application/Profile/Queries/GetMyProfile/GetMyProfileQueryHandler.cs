using EnglishPartner.Modules.Identity.Domain;
using EnglishPartner.Modules.Identity.Domain.Roles.Repositories;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Identity.Application.Profile.Queries.GetMyProfile;

internal sealed class GetMyProfileQueryHandler(
    IUserRepository userRepository,
    IUserProfileRepository profileRepository,
    IRoleRepository roleRepository) : IQueryHandler<GetMyProfileQuery, MyProfileResponse>
{
    public async Task<Result<MyProfileResponse>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<MyProfileResponse>(IdentityErrors.User.NotFoundById(request.UserId));

        var profile = await profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        var roleIds = user.UserRoles.Select(ur => ur.RoleId);
        var roleNames = await roleRepository.GetRoleNamesByIdsAsync(roleIds, cancellationToken);

        return new MyProfileResponse(
            user.Id,
            user.Email,
            user.DisplayName,
            profile?.NativeLanguage,
            profile?.TargetLanguage ?? "en",
            profile?.ProficiencyLevel,
            profile?.LearningGoal,
            profile?.DailyGoalMinutes ?? 15,
            profile?.AvatarUrl,
            profile?.DateOfBirth,
            user.IsOnboardingCompleted,
            roleNames);
    }
}
