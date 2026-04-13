using EnglishPartner.SharedKernel.Application.Caching;
using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Topics.Queries.GetTopics;

public sealed record GetTopicsQuery(string LanguageCode, string? SearchTerm = null) : ICacheableQuery<List<TopicResponse>>
{
    public string CacheKey => $"Topics:{LanguageCode}:Search:{SearchTerm?.ToLower()}";
    public TimeSpan? Expiration => TimeSpan.FromHours(24);
}

public sealed record TopicResponse(
    Guid Id,
    string Title,
    string? Description,
    string? Level,
    string? ImageUrl,
    int SortOrder);
