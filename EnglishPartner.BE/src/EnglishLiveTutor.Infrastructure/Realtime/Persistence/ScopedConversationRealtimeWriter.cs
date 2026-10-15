using System.Collections.Concurrent;
using EnglishLiveTutor.Application.Features.Conversations.Ports;
using EnglishLiveTutor.Domain.Conversations;
using EnglishLiveTutor.Domain.Conversations.Entities;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishLiveTutor.Infrastructure.Realtime.Persistence;

/// <summary>
/// Scoped-per-callback persistence adapter for realtime conversation events.
/// Creates a fresh DI scope (or DbContext via IDbContextFactory) per write operation,
/// sets the captured session actor on AppDbContext, and serializes writes per conversation.
/// </summary>
public sealed class ScopedConversationRealtimeWriter : IConversationRealtimeWriter
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _conversationLocks = new(StringComparer.Ordinal);

    public ScopedConversationRealtimeWriter(
        IDbContextFactory<AppDbContext> dbFactory,
        TimeProvider? timeProvider = null)
    {
        _dbFactory = dbFactory;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    private SemaphoreSlim GetConversationLock(string conversationId) =>
        _conversationLocks.GetOrAdd(conversationId ?? string.Empty, _ => new SemaphoreSlim(1, 1));

    public async Task<int> GetMaxSequenceOrderAsync(string conversationId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        bool exists = await db.Conversations.AsNoTracking().AnyAsync(c => c.Id == conversationId, ct);
        if (!exists)
        {
            return 0;
        }

        return await db.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
            .Select(m => (int?)m.SequenceOrder)
            .MaxAsync(ct) ?? 0;
    }

    public async Task EnsureConversationSettingsAsync(
        string conversationId,
        string? title,
        string cefrLevel,
        bool extendedThinking,
        string voiceName,
        double speechSpeed,
        string conversationMode,
        string topic,
        string? customSystemPrompt,
        string? actorUserId = null,
        CancellationToken ct = default)
    {
        var gate = GetConversationLock(conversationId);
        await gate.WaitAsync(ct);
        try
        {
            var now = _timeProvider.GetUtcNow();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            db.ActorOverride = string.IsNullOrWhiteSpace(actorUserId) ? "system" : actorUserId.Trim();

            var conv = await db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, ct);
            if (conv is null)
            {
                conv = ConversationEntity.Create(
                    id: conversationId,
                    title: title,
                    cefrLevel: cefrLevel,
                    extendedThinking: extendedThinking,
                    voiceName: voiceName,
                    speechSpeed: speechSpeed,
                    conversationMode: conversationMode,
                    topic: topic,
                    customSystemPrompt: customSystemPrompt,
                    createdAtUtc: now);
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
                    customSystemPrompt: customSystemPrompt,
                    updatedAtUtc: now);
            }

            await db.SaveChangesAsync(ct);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task UpsertMessageTranscriptAsync(
        string conversationId,
        string turnId,
        string speaker,
        int sequenceOrder,
        string fullTranscript,
        bool isFinal,
        string? actorUserId = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fullTranscript))
            return;

        var gate = GetConversationLock(conversationId);
        await gate.WaitAsync(ct);
        try
        {
            var now = _timeProvider.GetUtcNow();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            db.ActorOverride = string.IsNullOrWhiteSpace(actorUserId) ? "system" : actorUserId.Trim();

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

            conv.Touch(now);
            if (speaker == "user")
            {
                conv.UpdateTitleFromFirstUserMessage(fullTranscript, now);
            }

            await db.SaveChangesAsync(ct);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task UpsertMessageTranslationAsync(
        string conversationId,
        string turnId,
        string speaker,
        int sequenceOrder,
        string fullVietnameseText,
        string? actorUserId = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fullVietnameseText))
            return;

        var gate = GetConversationLock(conversationId);
        await gate.WaitAsync(ct);
        try
        {
            var now = _timeProvider.GetUtcNow();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            db.ActorOverride = string.IsNullOrWhiteSpace(actorUserId) ? "system" : actorUserId.Trim();

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

            conv.Touch(now);
            await db.SaveChangesAsync(ct);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task SaveCorrectionAsync(
        LanguageCorrectionEntity correction,
        string? actorUserId = null,
        CancellationToken ct = default)
    {
        var gate = GetConversationLock(correction.ConversationId);
        await gate.WaitAsync(ct);
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            db.ActorOverride = string.IsNullOrWhiteSpace(actorUserId) ? "system" : actorUserId.Trim();

            bool exists = await db.Conversations.AsNoTracking().AnyAsync(c => c.Id == correction.ConversationId, ct);
            if (!exists)
                return;

            db.Corrections.Add(correction);
            await db.SaveChangesAsync(ct);
        }
        finally
        {
            gate.Release();
        }
    }
}
