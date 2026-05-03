using EnglishPartner.Modules.Learning.Application.Abstractions;
using EnglishPartner.Modules.Learning.Domain.UserProfiles;
using EnglishPartner.Modules.Learning.Domain.UserProfiles.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Learning.Application.UserProfiles.Queries.GetDashboard;

internal sealed class GetUserDashboardQueryHandler(
    IUserLearningProfileRepository profileRepository,
    ILearningUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IQueryHandler<GetUserDashboardQuery, UserDashboardResponse>
{
    public async Task<Result<UserDashboardResponse>> Handle(GetUserDashboardQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        var profile = await profileRepository.GetByUserIdAsync(userId, cancellationToken);

        // Lazy init: auto-create profile if it doesn't exist yet
        // (covers race condition where UserRegisteredIntegrationEvent hasn't been processed)
        if (profile is null)
        {
            profile = UserLearningProfile.Create(
                userId: userId,
                email: currentUser.Email ?? string.Empty,
                displayName: "Learner");
            profileRepository.Add(profile);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new UserDashboardResponse(
            profile.UserId,
            profile.CurrentLevel,
            profile.TotalXp,
            profile.CurrentStreak,
            profile.LongestStreak);
    }
}
