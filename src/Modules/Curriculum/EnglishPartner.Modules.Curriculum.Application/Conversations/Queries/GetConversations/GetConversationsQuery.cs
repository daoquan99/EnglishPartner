using EnglishPartner.SharedKernel.Application.Caching;
using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Curriculum.Application.Conversations.Queries.GetConversations;

public sealed record GetConversationsQuery(string LanguageCode) : ICacheableQuery<List<ConversationListItemResponse>>
{
    public string CacheKey => $"Conversations:{LanguageCode}";
    public TimeSpan? Expiration => TimeSpan.FromHours(24);
}

public sealed record ConversationListItemResponse(
    Guid Id,
    string Title,
    string? Scenario,
    string? Level,
    string? ImageUrl);
