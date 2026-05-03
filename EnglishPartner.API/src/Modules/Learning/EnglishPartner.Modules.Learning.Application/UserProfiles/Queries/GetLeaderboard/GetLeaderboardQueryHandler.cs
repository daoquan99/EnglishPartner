using EnglishPartner.Modules.Learning.Domain.UserProfiles.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Learning.Application.UserProfiles.Queries.GetLeaderboard;

internal sealed class GetLeaderboardQueryHandler(
    IUserLearningProfileRepository profileRepository) : IQueryHandler<GetLeaderboardQuery, List<LeaderboardEntryResponse>>
{
    public async Task<Result<List<LeaderboardEntryResponse>>> Handle(GetLeaderboardQuery request, CancellationToken cancellationToken)
    {
        var count = request.TopCount > 0 && request.TopCount <= 100 ? request.TopCount : 50;

        var entries = await profileRepository.GetLeaderboardAsync(count, cancellationToken);

        var result = entries.Select(x => new LeaderboardEntryResponse(
            x.UserId,
            string.IsNullOrWhiteSpace(x.DisplayName) ? "Anonymous Learner" : x.DisplayName,
            x.TotalXp,
            x.CurrentLevel,
            x.CurrentStreak
        )).ToList();

        return result;
    }
}
