using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Conversations;
using EnglishLiveTutor.Domain.Conversations.Repositories;

namespace EnglishLiveTutor.Application.Features.Conversations.Queries.GetConversationHistory;

public sealed class GetConversationHistoryQueryHandler : IQueryHandler<GetConversationHistoryQuery, ConversationDetailDto?>
{
    private readonly IConversationRepository _repo;
    private readonly ICurrentUser _currentUser;

    public GetConversationHistoryQueryHandler(IConversationRepository repo, ICurrentUser currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<ConversationDetailDto?> HandleAsync(GetConversationHistoryQuery query, CancellationToken ct = default)
    {
        string userId = _currentUser.UserId ?? throw new UnauthorizedAccessException("Vui lòng đăng nhập.");
        var conv = await _repo.GetConversationDetailAsync(userId, query.Id, ct);
        if (conv is null)
        {
            return null;
        }

        var messages = conv.Messages.Select(m => new ChatMessageDto(
            m.Id,
            m.ConversationId,
            m.TurnId,
            m.Speaker,
            m.TranscriptLine1,
            m.TranslationLine2,
            m.IsFinal,
            m.SequenceOrder,
            m.CreatedAtUtc
        )).ToList();

        var corrections = conv.Corrections.Select(c =>
        {
            IReadOnlyList<LanguageCorrectionIssueDto>? issues = null;
            if (!string.IsNullOrWhiteSpace(c.IssuesJson))
            {
                try
                {
                    issues = System.Text.Json.JsonSerializer.Deserialize<List<LanguageCorrectionIssueDto>>(c.IssuesJson);
                }
                catch
                {
                    issues = null;
                }
            }

            return new LanguageCorrectionDto(
                c.Id,
                c.ConversationId,
                c.TurnId,
                c.OriginalText,
                c.CorrectedText,
                c.ErrorCategory,
                c.ExplanationVi,
                c.CefrTip,
                c.CreatedAtUtc,
                c.Status,
                c.ImprovedText,
                issues
            );
        }).ToList();

        return new ConversationDetailDto(
            conv.Id,
            conv.Title,
            conv.CefrLevel,
            conv.ExtendedThinking,
            conv.VoiceName,
            conv.SpeechSpeed,
            conv.ConversationMode,
            conv.Topic,
            conv.CustomSystemPrompt,
            conv.CreatedAtUtc,
            conv.UpdatedAtUtc,
            messages,
            corrections
        );
    }
}
