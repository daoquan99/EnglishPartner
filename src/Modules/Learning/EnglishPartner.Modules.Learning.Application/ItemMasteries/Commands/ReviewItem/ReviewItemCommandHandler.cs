using EnglishPartner.Modules.Learning.Domain.ItemMasteries;
using EnglishPartner.Modules.Learning.Domain.ItemMasteries.Repositories;
using EnglishPartner.Modules.Learning.Domain.UserProfiles.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Learning.Application.ItemMasteries.Commands.ReviewItem;

internal sealed class ReviewItemCommandHandler(
    IItemMasteryRepository masteryRepository,
    IUserLearningProfileRepository profileRepository) : ICommandHandler<ReviewItemCommand>
{
    public async Task<Result> Handle(ReviewItemCommand request, CancellationToken cancellationToken)
    {
        var mastery = await masteryRepository.GetAsync(request.UserId, request.ItemId, cancellationToken);
        if (mastery is null)
        {
            mastery = ItemMastery.Create(request.UserId, request.ItemId, request.Type);
            masteryRepository.Add(mastery);
        }

        mastery.Review(request.QualityRating, DateTime.UtcNow);
        
        var profile = await profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile is not null)
        {
            profile.RecordActivity(DateTime.UtcNow);
            profile.AddXp(request.QualityRating * 2); // basic logic for xp grant
            profileRepository.Update(profile);
        }

        return Result.Success();
    }
}
