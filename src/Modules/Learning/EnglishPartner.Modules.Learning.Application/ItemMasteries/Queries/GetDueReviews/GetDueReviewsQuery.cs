using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Learning.Application.ItemMasteries.Queries.GetDueReviews;

public sealed record GetDueReviewsQuery(Guid UserId, DateTime OnOrBefore) : IQuery<List<DueReviewResponse>>;

public sealed record DueReviewResponse(Guid ItemId, int Type, double EasinessFactor, int IntervalDays, int Repetitions);
