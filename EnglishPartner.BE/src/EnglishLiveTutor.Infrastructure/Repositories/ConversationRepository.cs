using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Conversations;
using EnglishLiveTutor.Domain.Conversations.Entities;
using EnglishLiveTutor.Domain.Conversations.Repositories;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Repositories;

public sealed class ConversationRepository : IConversationRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ICurrentUser _currentUser;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public ConversationRepository(
        IDbContextFactory<AppDbContext> dbFactory,
        TimeProvider? timeProvider = null,
        ICurrentUser? currentUser = null)
    {
        _dbFactory = dbFactory;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _currentUser = currentUser ?? SystemCurrentUser.Instance;
    }

    public async Task<List<object>> ListConversationsAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var items = await db.Conversations
            .AsNoTracking()
            .OrderByDescending(c => c.UpdatedAtUtc ?? c.CreatedAtUtc)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.CefrLevel,
                c.ExtendedThinking,
                c.VoiceName,
                c.SpeechSpeed,
                c.ConversationMode,
                c.Topic,
                c.CustomSystemPrompt,
                CreatedAt = c.CreatedAtUtc,
                UpdatedAt = c.UpdatedAtUtc ?? c.CreatedAtUtc,
                c.CreatedAtUtc,
                c.UpdatedAtUtc,
                MessageCount = c.Messages.Count(m => m.TranscriptLine1 != "" || m.TranslationLine2 != ""),
                LastMessagePreview = c.Messages
                    .Where(m => m.TranscriptLine1 != "")
                    .OrderByDescending(m => m.SequenceOrder)
                    .Select(m => m.TranscriptLine1)
                    .FirstOrDefault() ?? "Chưa có tin nhắn"
            })
            .ToListAsync(ct);

        return items.Cast<object>().ToList();
    }

    public async Task<ConversationEntity?> GetConversationWithHistoryAsync(string id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var conv = await db.Conversations
            .AsNoTracking()
            .Include(c => c.Messages
                .Where(m => m.TranscriptLine1 != "" || m.TranslationLine2 != "")
                .OrderBy(m => m.SequenceOrder))
            .Include(c => c.Corrections.OrderByDescending(x => x.CreatedAtUtc))
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        return conv;
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

    public async Task<ConversationEntity> CreateOrUpdateConversationAsync(
        string? id,
        string? title,
        string cefrLevel,
        bool extendedThinking,
        string voiceName,
        double speechSpeed,
        string conversationMode = "daily_conversation",
        string topic = "",
        string? customSystemPrompt = null,
        CancellationToken ct = default)
    {
        await _writeLock.WaitAsync(ct);
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            ConversationEntity? conv = null;
            if (!string.IsNullOrWhiteSpace(id))
            {
                conv = await db.Conversations.FirstOrDefaultAsync(c => c.Id == id, ct);
            }

            if (conv is null)
            {
                conv = ConversationEntity.Create(
                    id: id,
                    title: title,
                    cefrLevel: cefrLevel,
                    extendedThinking: extendedThinking,
                    voiceName: voiceName,
                    speechSpeed: speechSpeed,
                    conversationMode: conversationMode,
                    topic: topic,
                    customSystemPrompt: customSystemPrompt);
                db.Conversations.Add(conv);
            }
            else
            {
                conv.UpdateSettings(
                    title: title,
                    cefrLevel: cefrLevel,
                    extendedThinking: extendedThinking,
                    voiceName: voiceName,
                    speechSpeed: speechSpeed,
                    conversationMode: conversationMode,
                    topic: topic,
                    customSystemPrompt: customSystemPrompt);
            }

            await db.SaveChangesAsync(ct);
            return conv;
        }
        finally
        {
            _writeLock.Release();
        }
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
                conv.UpdateTitleFromFirstUserMessage(fullTranscript);
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

    public async Task SaveCorrectionAsync(LanguageCorrectionEntity correction, CancellationToken ct = default)
    {
        await _writeLock.WaitAsync(ct);
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            bool exists = await db.Conversations.AnyAsync(c => c.Id == correction.ConversationId, ct);
            if (!exists)
                return;

            db.Corrections.Add(correction);
            await db.SaveChangesAsync(ct);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<bool> DeleteConversationAsync(string id, CancellationToken ct = default)
    {
        await _writeLock.WaitAsync(ct);
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var conv = await db.Conversations.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (conv is null)
                return false;

            conv.SoftDelete(_timeProvider.GetUtcNow(), _currentUser.UserIdOrSystem);
            await db.SaveChangesAsync(ct);
            return true;
        }
        finally
        {
            _writeLock.Release();
        }
    }
}
