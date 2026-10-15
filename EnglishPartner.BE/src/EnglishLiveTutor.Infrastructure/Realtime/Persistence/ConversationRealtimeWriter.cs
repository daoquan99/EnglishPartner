using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Realtime;

/// <summary>
/// Per-operation persistence adapter for live SignalR sessions.
/// Creates a short-lived AppDbContext from IDbContextFactory on each callback instead of holding
/// a scoped repository or DbContext inside a singleton manager.
/// </summary>
public sealed class ConversationRealtimeWriter : IConversationRealtimeWriter
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public ConversationRealtimeWriter(
        IDbContextFactory<AppDbContext> dbFactory,
        TimeProvider? timeProvider = null)
    {
        _dbFactory = dbFactory;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ConversationRealtimeContextDto> EnsureConversationForSessionAsync(
        StartTutorSessionRequest request,
        CancellationToken ct = default)
    {
        await _writeLock.WaitAsync(ct);
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var conv = await db.Conversations
                .Include(c => c.Messages
                    .Where(m => m.TranscriptLine1 != "" || m.TranslationLine2 != "")
                    .OrderBy(m => m.SequenceOrder))
                .FirstOrDefaultAsync(c => c.Id == request.ConversationId, ct);

            if (conv is null)
            {
                conv = ConversationEntity.Create(
                    id: request.ConversationId,
                    title: "Hội thoại mới",
                    cefrLevel: request.CefrLevel,
                    extendedThinking: request.ExtendedThinking,
                    voiceName: request.VoiceName,
                    speechSpeed: request.SpeechSpeed,
                    conversationMode: request.ConversationMode,
                    topic: request.Topic,
                    customSystemPrompt: request.CustomSystemPrompt);
                db.Conversations.Add(conv);
            }
            else
            {
                conv.UpdateSettings(
                    title: null,
                    cefrLevel: request.CefrLevel,
                    extendedThinking: request.ExtendedThinking,
                    voiceName: request.VoiceName,
                    speechSpeed: request.SpeechSpeed,
                    conversationMode: request.ConversationMode,
                    topic: request.Topic,
                    customSystemPrompt: request.CustomSystemPrompt);
            }

            await db.SaveChangesAsync(ct);

            var recentMessages = conv.Messages
                .OrderBy(m => m.SequenceOrder)
                .ToList();
            int maxSeq = recentMessages.Count == 0 ? 0 : recentMessages.Max(m => m.SequenceOrder);

            return new ConversationRealtimeContextDto(
                ConversationId: conv.Id,
                CefrLevel: conv.CefrLevel,
                MaxSequenceOrder: maxSeq,
                RecentMessages: recentMessages);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<int> GetMaxSequenceOrderAsync(string conversationId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        bool exists = await db.Conversations.AnyAsync(c => c.Id == conversationId, ct);
        if (!exists)
        {
            return 0;
        }

        return await db.Messages
            .Where(m => m.ConversationId == conversationId)
            .Select(m => (int?)m.SequenceOrder)
            .MaxAsync(ct) ?? 0;
    }

    public async Task UpsertMessageTranscriptAsync(
        string conversationId,
        string turnId,
        string speaker,
        int sequenceOrder,
        string fullTranscript,
        bool isFinal,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fullTranscript))
            return;

        await _writeLock.WaitAsync(ct);
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var conv = await db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, ct);
            if (conv is null)
                return;

            var msg = await db.Messages.FirstOrDefaultAsync(
                m => m.ConversationId == conversationId && m.TurnId == turnId && m.Speaker == speaker,
                ct);

            if (msg is null)
            {
                msg = ChatMessageEntity.CreateWithTranscript(
                    conversationId: conversationId,
                    turnId: turnId,
                    speaker: speaker,
                    sequenceOrder: sequenceOrder,
                    fullTranscript: fullTranscript,
                    isFinal: isFinal);
                db.Messages.Add(msg);
            }
            else
            {
                msg.UpdateTranscript(fullTranscript, isFinal, sequenceOrder);
            }

            conv.Touch(_timeProvider.GetUtcNow());
            if (speaker == "user")
            {
                string candidateTitle = ConversationMapping.FormatPreviewTitleFromUtterance(fullTranscript);
                conv.UpdateTitleFromFirstUserMessage(candidateTitle);
            }

            await db.SaveChangesAsync(ct);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task UpsertMessageTranslationAsync(
        string conversationId,
        string turnId,
        string speaker,
        int sequenceOrder,
        string fullVietnameseText,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fullVietnameseText))
            return;

        await _writeLock.WaitAsync(ct);
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var conv = await db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, ct);
            if (conv is null)
                return;

            var msg = await db.Messages.FirstOrDefaultAsync(
                m => m.ConversationId == conversationId && m.TurnId == turnId && m.Speaker == speaker,
                ct);

            if (msg is null)
            {
                msg = ChatMessageEntity.CreateWithTranslation(
                    conversationId: conversationId,
                    turnId: turnId,
                    speaker: speaker,
                    sequenceOrder: sequenceOrder,
                    fullVietnameseText: fullVietnameseText);
                db.Messages.Add(msg);
            }
            else
            {
                msg.UpdateTranslation(fullVietnameseText, sequenceOrder);
            }

            conv.Touch(_timeProvider.GetUtcNow());
            await db.SaveChangesAsync(ct);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task SaveCorrectionAsync(LanguageCorrectionDto correction, CancellationToken ct = default)
    {
        await _writeLock.WaitAsync(ct);
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            bool exists = await db.Conversations.AnyAsync(c => c.Id == correction.ConversationId, ct);
            if (!exists)
                return;

            var entity = new LanguageCorrectionEntity(
                id: correction.Id,
                conversationId: correction.ConversationId,
                turnId: correction.TurnId,
                originalText: correction.OriginalText,
                correctedText: correction.CorrectedText,
                errorCategory: correction.ErrorCategory,
                explanationVi: correction.ExplanationVi,
                cefrTip: correction.CefrTip,
                createdAtUtc: correction.Timestamp);

            db.Corrections.Add(entity);
            await db.SaveChangesAsync(ct);
        }
        finally
        {
            _writeLock.Release();
        }
    }
}
