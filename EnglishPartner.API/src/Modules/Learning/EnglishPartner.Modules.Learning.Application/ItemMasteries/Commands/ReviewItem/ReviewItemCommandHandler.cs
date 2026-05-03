using EnglishPartner.Modules.Learning.Domain.ItemMasteries;
using EnglishPartner.Modules.Learning.Domain.ItemMasteries.Repositories;
using EnglishPartner.Modules.Learning.Domain.UserProfiles.Repositories;
using EnglishPartner.Modules.Learning.Domain.XpPolicy;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Learning.Application.ItemMasteries.Commands.ReviewItem;

internal sealed class ReviewItemCommandHandler(
    IItemMasteryRepository masteryRepository,
    IUserLearningProfileRepository profileRepository,
    TimeProvider timeProvider,
    ICurrentUserService currentUser) : ICommandHandler<ReviewItemCommand>
{
    public async Task<Result> Handle(ReviewItemCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var mastery = await masteryRepository.GetAsync(userId, request.ItemId, cancellationToken);
        if (mastery is null)
        {
            mastery = ItemMastery.Create(userId, request.ItemId, request.Type, utcNow);
            masteryRepository.Add(mastery);
        }
        else
        {
            masteryRepository.Update(mastery);
        }

        mastery.Review(request.QualityRating, utcNow);
        
        var profile = await profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile is not null)
        {
            profile.RecordActivity(utcNow);
            profile.AddXp(XpPolicy.FromStandaloneReview());
            profileRepository.Update(profile);
        }

        return Result.Success();
    }
}
