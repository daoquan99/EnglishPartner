using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Conversations.Repositories;

namespace EnglishLiveTutor.Application.Features.Conversations.Queries.GetConversationHistory;

public sealed record GetConversationHistoryQuery(string Id) : IQuery<object?>;

public sealed class GetConversationHistoryQueryHandler : IQueryHandler<GetConversationHistoryQuery, object?>
{
    private readonly IConversationRepository _repo;

    public GetConversationHistoryQueryHandler(IConversationRepository repo)
    {
        _repo = repo;
    }

    public async Task<object?> HandleAsync(GetConversationHistoryQuery query, CancellationToken ct = default)
    {
        return await _repo.GetConversationWithHistoryAsync(query.Id, ct);
    }
}
