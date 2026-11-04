using EnglishLiveTutor.Application.Common.Interfaces;

namespace EnglishLiveTutor.Application.Features.Progress.Queries.GetProgressOverview;

public sealed record GetProgressOverviewQuery(string UserId, int Days = 30) : IQuery<ProgressOverviewDto>;
