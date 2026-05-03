using EnglishPartner.Modules.AI.Domain.ChatSessions.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.AI.Application.Chat.Queries.GetMySessions;

internal sealed class GetMySessionsQueryHandler(
    IChatSessionRepository repository,
    ICurrentUserService currentUser) : IQueryHandler<GetMySessionsQuery, List<ChatSessionSummaryDto>>
{
    public async Task<Result<List<ChatSessionSummaryDto>>> Handle(GetMySessionsQuery request, CancellationToken cancellationToken)
    {
        var summaries = await repository.GetUserSessionSummariesAsync(currentUser.UserId!.Value, cancellationToken);

        var dtos = summaries
            .Select(s => new ChatSessionSummaryDto(s.Id, s.Title, s.SystemPrompt, s.Topic, s.SessionType.ToString(), s.VoiceName, s.MessageCount, s.CreatedAt, s.UpdatedAt))
            .ToList();

        return dtos;
    }
}
