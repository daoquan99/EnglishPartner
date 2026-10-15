using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Conversations.Repositories;

namespace EnglishLiveTutor.Application.Features.Conversations.Queries.ListConversations;

public sealed record ListConversationsQuery(
    string? Search = null,
    int? Page = null,
    int? PageSize = null
) : IQuery<PagedResultDto<object>>;

public sealed class ListConversationsQueryHandler : IQueryHandler<ListConversationsQuery, PagedResultDto<object>>
{
    private readonly IConversationRepository _repo;

    public ListConversationsQueryHandler(IConversationRepository repo)
    {
        _repo = repo;
    }

    public async Task<PagedResultDto<object>> HandleAsync(ListConversationsQuery query, CancellationToken ct = default)
    {
        var (items, totalCount) = await _repo.ListConversationsPagedAsync(
            query.Search,
            query.Page,
            query.PageSize,
            ct);

        return PagedResultDto<object>.FromPage(items, totalCount, query.Page, query.PageSize);
    }
}
