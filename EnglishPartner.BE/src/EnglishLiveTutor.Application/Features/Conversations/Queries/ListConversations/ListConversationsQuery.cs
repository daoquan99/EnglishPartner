using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Conversations.Repositories;

namespace EnglishLiveTutor.Application.Features.Conversations.Queries.ListConversations;

public sealed record ListConversationsQuery() : IQuery<List<object>>;

public sealed class ListConversationsQueryHandler : IQueryHandler<ListConversationsQuery, List<object>>
{
    private readonly IConversationRepository _repo;

    public ListConversationsQueryHandler(IConversationRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<object>> HandleAsync(ListConversationsQuery query, CancellationToken ct = default)
    {
        return await _repo.ListConversationsAsync(ct);
    }
}
