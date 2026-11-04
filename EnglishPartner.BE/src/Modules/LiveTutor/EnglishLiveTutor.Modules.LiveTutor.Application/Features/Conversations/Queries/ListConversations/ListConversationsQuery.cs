using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;

namespace EnglishLiveTutor.Application.Features.Conversations.Queries.ListConversations;

public sealed record ConversationSummaryDto(
    string Id,
    string Title,
    string CefrLevel,
    bool ExtendedThinking,
    string VoiceName,
    double SpeechSpeed,
    string ConversationMode,
    string Topic,
    string? CustomSystemPrompt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    int MessageCount,
    int CorrectionCount,
    string LastMessagePreview
);

public sealed record ListConversationsQuery(string? Search = null, int? Page = null, int? PageSize = null) 
    : IQuery<PagedResultDto<ConversationSummaryDto>>;
