using EnglishPartner.Modules.AI.Domain.ChatSessions.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.AI.Application.Chat.Queries.GetMySessions;

internal sealed class GetMySessionsQueryHandler(
    IChatSessionRepository repository) : IQueryHandler<GetMySessionsQuery, List<ChatSessionSummaryDto>>
{
    public async Task<Result<List<ChatSessionSummaryDto>>> Handle(GetMySessionsQuery request, CancellationToken cancellationToken)
    {
        var sessions = await repository.GetUserSessionsAsync(request.UserId, cancellationToken);

        var dtos = sessions
            .Select(s => new ChatSessionSummaryDto(
                s.Id,
                s.Title,
                s.Messages.Count,
                s.CreatedAt,
                s.UpdatedAt))
            .ToList();

        return dtos;
    }
}
