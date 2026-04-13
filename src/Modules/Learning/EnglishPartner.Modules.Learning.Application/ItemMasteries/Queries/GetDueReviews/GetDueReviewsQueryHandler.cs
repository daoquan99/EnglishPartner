using EnglishPartner.Modules.Learning.Domain.ItemMasteries.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Learning.Application.ItemMasteries.Queries.GetDueReviews;

internal sealed class GetDueReviewsQueryHandler(
    IItemMasteryRepository masteryRepository) : IQueryHandler<GetDueReviewsQuery, List<DueReviewResponse>>
{
    public async Task<Result<List<DueReviewResponse>>> Handle(GetDueReviewsQuery request, CancellationToken cancellationToken)
    {
        var dueItems = await masteryRepository.GetDueReviewsAsync(request.UserId, request.OnOrBefore, cancellationToken);
        
        return dueItems.Select(x => new DueReviewResponse(
            x.ItemId, (int)x.Type, x.EasinessFactor, x.IntervalDays, x.Repetitions)).ToList();
    }
}
