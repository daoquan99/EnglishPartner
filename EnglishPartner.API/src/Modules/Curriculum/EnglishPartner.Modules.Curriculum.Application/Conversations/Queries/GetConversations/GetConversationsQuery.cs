using EnglishPartner.SharedKernel.Application.Caching;
using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Conversations.Queries.GetConversations;

public sealed record GetConversationsQuery(
    string LanguageCode,
    int Page = 1,
    int PageSize = 20,
    Guid? TopicId = null,
    string? Level = null) : ICacheableQuery<ConversationsResponse>
{
    public string CacheKey => CacheKeys.Curriculum.ConversationsQuery(LanguageCode, Page, PageSize, TopicId, Level);
    public TimeSpan? Expiration => TimeSpan.FromHours(24);
}

public sealed record ConversationsResponse(
    List<ConversationListItemResponse> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record ConversationListItemResponse(
    Guid Id,
    string Title,
    string? Scenario,
    string? Level,
    string? ImageUrl);
