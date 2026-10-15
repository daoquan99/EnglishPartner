using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Conversations;
using EnglishLiveTutor.Domain.Conversations.Repositories;

namespace EnglishLiveTutor.Application.Features.Conversations.Queries.ListConversations;

public sealed class ListConversationsQueryHandler : IQueryHandler<ListConversationsQuery, PagedResultDto<ConversationSummaryDto>>
{
    private readonly IConversationRepository _repo;

    public ListConversationsQueryHandler(IConversationRepository repo)
    {
        _repo = repo;
    }

    public async Task<PagedResultDto<ConversationSummaryDto>> HandleAsync(ListConversationsQuery query, CancellationToken ct = default)
    {
        var (items, totalCount) = await _repo.ListConversationsPagedAsync(
            query.Search,
            query.Page,
            query.PageSize,
            ct);

        var dtos = items.Select(c => new ConversationSummaryDto(
            c.Id,
            c.Title,
            c.CefrLevel,
            c.ExtendedThinking,
            c.VoiceName,
            c.SpeechSpeed,
            c.ConversationMode,
            c.Topic,
            c.CustomSystemPrompt,
            c.CreatedAt,
            c.UpdatedAt,
            c.CreatedAtUtc,
            c.UpdatedAtUtc,
            c.MessageCount,
            c.CorrectionCount,
            c.LastMessagePreview
        )).ToList();

        return PagedResultDto<ConversationSummaryDto>.FromPage(dtos, totalCount, query.Page, query.PageSize);
    }
}
