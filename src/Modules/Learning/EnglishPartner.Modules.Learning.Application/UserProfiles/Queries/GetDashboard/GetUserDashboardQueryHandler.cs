using EnglishPartner.Modules.Learning.Domain.UserProfiles.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Learning.Application.UserProfiles.Queries.GetDashboard;

internal sealed class GetUserDashboardQueryHandler(
    IUserLearningProfileRepository profileRepository) : IQueryHandler<GetUserDashboardQuery, UserDashboardResponse>
{
    public async Task<Result<UserDashboardResponse>> Handle(GetUserDashboardQuery request, CancellationToken cancellationToken)
    {
        var profile = await profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile is null) return Result.Failure<UserDashboardResponse>(CommonErrors.NotFound("LearningProfile", request.UserId));

        return new UserDashboardResponse(
            profile.UserId,
            profile.CurrentLevel,
            profile.TotalXp,
            profile.CurrentStreak,
            profile.LongestStreak);
    }
}
