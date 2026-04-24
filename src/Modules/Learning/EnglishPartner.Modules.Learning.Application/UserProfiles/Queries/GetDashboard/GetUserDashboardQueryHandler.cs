using EnglishPartner.Modules.Learning.Domain.UserProfiles.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Learning.Application.UserProfiles.Queries.GetDashboard;

internal sealed class GetUserDashboardQueryHandler(
    IUserLearningProfileRepository profileRepository,
    ICurrentUserService currentUser) : IQueryHandler<GetUserDashboardQuery, UserDashboardResponse>
{
    public async Task<Result<UserDashboardResponse>> Handle(GetUserDashboardQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        var profile = await profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is null) return Result.Failure<UserDashboardResponse>(CommonErrors.NotFound("LearningProfile", userId));

        return new UserDashboardResponse(
            profile.UserId,
            profile.CurrentLevel,
            profile.TotalXp,
            profile.CurrentStreak,
            profile.LongestStreak);
    }
}
