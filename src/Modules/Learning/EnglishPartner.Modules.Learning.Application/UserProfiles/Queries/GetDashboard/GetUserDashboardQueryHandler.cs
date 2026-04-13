using EnglishPartner.Modules.Learning.Domain.UserProfiles.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;
using Error = EnglishPartner.SharedKernel.Results.Error;

namespace EnglishPartner.Modules.Learning.Application.UserProfiles.Queries.GetDashboard;

internal sealed class GetUserDashboardQueryHandler(
    IUserLearningProfileRepository profileRepository) : IQueryHandler<GetUserDashboardQuery, UserDashboardResponse>
{
    public async Task<Result<UserDashboardResponse>> Handle(GetUserDashboardQuery request, CancellationToken cancellationToken)
    {
        var profile = await profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile is null) return Result.Failure<UserDashboardResponse>(new Error("LearningProfile.NotFound", "User learning profile not found."));

        return new UserDashboardResponse(
            profile.UserId,
            profile.CurrentLevel,
            profile.TotalXp,
            profile.CurrentStreak,
            profile.LongestStreak);
    }
}
