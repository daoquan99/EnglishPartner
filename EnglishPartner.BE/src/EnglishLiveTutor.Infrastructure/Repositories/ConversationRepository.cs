using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Conversations;
using EnglishLiveTutor.Domain.Conversations.Entities;
using EnglishLiveTutor.Domain.Conversations.Repositories;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Repositories;

public sealed class ConversationRepository : IConversationRepository
{
    private static readonly object _inMemoryLock = new();
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly TimeProvider _timeProvider;

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        for (Exception? cur = ex; cur != null; cur = cur.InnerException)
        {
            if (cur is Microsoft.Data.SqlClient.SqlException sqlEx &&
                (sqlEx.Number == 2627 || sqlEx.Number == 2601))
            {
                return true;
            }

            if (cur is ArgumentException argEx &&
                argEx.Message.Contains("same key has already been added", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string msg = cur.Message;
            if (msg.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("Primary key constraint violation", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    public ConversationRepository(
        IDbContextFactory<AppDbContext> dbFactory,
        TimeProvider? timeProvider = null)
    {
        _dbFactory = dbFactory;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<List<ConversationSummary>> ListConversationsAsync(string userId, CancellationToken ct = default)
    {
        var (items, _) = await ListConversationsPagedAsync(userId, null, null, null, ct);
        return items;
    }

    public async Task<(List<ConversationSummary> Items, int TotalCount)> ListConversationsPagedAsync(
        string userId,
        string? search = null,
        int? page = null,
        int? pageSize = null,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var query = db.Conversations
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLowerInvariant();
            query = query.Where(c =>
                c.Title.ToLower().Contains(term) ||
                c.Topic.ToLower().Contains(term) ||
                c.ConversationMode.ToLower().Contains(term) ||
                c.CefrLevel.ToLower().Contains(term) ||
                c.VoiceName.ToLower().Contains(term));
        }

        int totalCount = await query.CountAsync(ct);

        var projected = query
            .OrderByDescending(c => c.UpdatedAtUtc ?? c.CreatedAtUtc)
            .Select(c => new ConversationSummary(
                c.Id,
                c.Title,
                c.CefrLevel,
                c.ExtendedThinking,
                c.VoiceName,
                c.SpeechSpeed,
                c.ConversationMode,
                c.Topic,
                c.CustomSystemPrompt,
                c.CreatedAtUtc,
                c.UpdatedAtUtc ?? c.CreatedAtUtc,
                c.CreatedAtUtc,
                c.UpdatedAtUtc,
                c.Messages.Count(m => m.TranscriptLine1 != "" || m.TranslationLine2 != ""),
                c.Corrections.Count(),
                c.Messages
                    .Where(m => m.TranscriptLine1 != "")
                    .OrderByDescending(m => m.SequenceOrder)
                    .Select(m => m.TranscriptLine1)
                    .FirstOrDefault() ?? "Chưa có tin nhắn"
            ));

        bool hasPaging = page.HasValue || pageSize.HasValue;
        if (hasPaging)
        {
            int effPage = Math.Max(1, page ?? 1);
            int effSize = Math.Clamp(pageSize ?? PagedResultDto<ConversationSummary>.DefaultPageSize, 1, PagedResultDto<ConversationSummary>.MaxPageSize);
            var pagedItems = await projected
                .Skip((effPage - 1) * effSize)
                .Take(effSize)
                .ToListAsync(ct);
            return (pagedItems, totalCount);
        }

        var items = await projected.Take(PagedResultDto<ConversationSummary>.MaxUnpagedItems).ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task<ConversationEntity?> GetConversationWithHistoryAsync(string userId, string id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var conv = await db.Conversations
            .AsNoTracking()
            .AsSplitQuery()
            .Include(c => c.Messages
                .Where(m => m.TranscriptLine1 != "" || m.TranslationLine2 != "")
                .OrderBy(m => m.SequenceOrder))
            .Include(c => c.Corrections.OrderByDescending(x => x.CreatedAtUtc))
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);

        return conv;
    }

    public async Task<IReadOnlyList<ChatMessageEntity>> GetRecentMessagesAsync(string userId, string conversationId, int take, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        // Live session start only needs the tail of the transcript; read it in SQL instead of loading every message and correction.
        var latest = await db.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId
                && db.Conversations.Any(c => c.Id == conversationId && c.UserId == userId)
                && (m.TranscriptLine1 != "" || m.TranslationLine2 != ""))
            .OrderByDescending(m => m.SequenceOrder)
            .Take(take)
            .ToListAsync(ct);

        latest.Reverse();
        return latest;
    }

    public async Task<ConversationEntity?> GetConversationByIdAsync(string id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.Conversations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
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

    public async Task<ConversationEntity> CreateConversationAsync(
        string userId,
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
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var conv = ConversationEntity.Create(
            userId: userId,
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
        try
        {
            await db.SaveChangesAsync(ct);
            return conv;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new NotFoundException("Không tìm thấy hội thoại.");
        }
    }

    public async Task<ConversationEntity?> UpdateConversationSettingsAsync(
        string userId,
        string id,
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
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var conv = await db.Conversations.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);
        if (conv is null)
        {
            return null;
        }

        conv.UpdateSettings(
            title: title,
            cefrLevel: cefrLevel,
            extendedThinking: extendedThinking,
            voiceName: voiceName,
            speechSpeed: speechSpeed,
            conversationMode: conversationMode,
            topic: topic,
            customSystemPrompt: customSystemPrompt);

        await db.SaveChangesAsync(ct);
        return conv;
    }

    public async Task<ConversationEntity> CreateOrUpdateConversationAsync(
        string userId,
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
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        ConversationEntity? conv = null;
        if (!string.IsNullOrWhiteSpace(id))
        {
            conv = await db.Conversations.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id, ct);
        }

        if (conv is null)
        {
            conv = ConversationEntity.Create(
                userId: userId,
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
            if (conv.IsDeleted || conv.UserId != userId)
            {
                throw new NotFoundException("Không tìm thấy hội thoại.");
            }

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

        try
        {
            await db.SaveChangesAsync(ct);
            return conv;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new NotFoundException("Không tìm thấy hội thoại.");
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

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (db.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            lock (_inMemoryLock)
            {
                var conv = db.Conversations.FirstOrDefault(c => c.Id == conversationId);
                if (conv is null) return;

                var msg = db.Messages.FirstOrDefault(
                    m => m.ConversationId == conversationId && m.TurnId == turnId && m.Speaker == speaker);

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
                    if (!isFinal && msg.IsFinal)
                    {
                        return;
                    }
                    msg.UpdateTranscript(fullTranscript, isFinal, sequenceOrder);
                }

                conv.Touch(_timeProvider.GetUtcNow());
                if (speaker == "user")
                {
                    conv.UpdateTitleFromFirstUserMessage(fullTranscript);
                }
                db.SaveChanges();
                return;
            }
        }

        var convEntity = await db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, ct);
        if (convEntity is null)
            return;

        if (!isFinal)
        {
            int rows = await db.Messages
                .Where(m => m.ConversationId == conversationId && m.TurnId == turnId && m.Speaker == speaker && !m.IsFinal)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(m => m.TranscriptLine1, fullTranscript)
                    .SetProperty(m => m.SequenceOrder, m => (sequenceOrder > 0 && (m.SequenceOrder == 0 || sequenceOrder < m.SequenceOrder)) ? sequenceOrder : m.SequenceOrder),
                    ct);

            if (rows > 0)
            {
                convEntity.Touch(_timeProvider.GetUtcNow());
                if (speaker == "user") convEntity.UpdateTitleFromFirstUserMessage(fullTranscript);
                await db.SaveChangesAsync(ct);
                return;
            }

            bool alreadyFinal = await db.Messages.AnyAsync(
                m => m.ConversationId == conversationId && m.TurnId == turnId && m.Speaker == speaker && m.IsFinal, ct);
            if (alreadyFinal)
            {
                return;
            }

            var newMsg = ChatMessageEntity.CreateWithTranscript(
                conversationId: conversationId,
                turnId: turnId,
                speaker: speaker,
                sequenceOrder: sequenceOrder,
                fullTranscript: fullTranscript,
                isFinal: false);
            db.Messages.Add(newMsg);
            convEntity.Touch(_timeProvider.GetUtcNow());
            if (speaker == "user") convEntity.UpdateTitleFromFirstUserMessage(fullTranscript);

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                await using var retryDb = await _dbFactory.CreateDbContextAsync(ct);
                await retryDb.Messages
                    .Where(m => m.ConversationId == conversationId && m.TurnId == turnId && m.Speaker == speaker && !m.IsFinal)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(m => m.TranscriptLine1, fullTranscript)
                        .SetProperty(m => m.SequenceOrder, m => (sequenceOrder > 0 && (m.SequenceOrder == 0 || sequenceOrder < m.SequenceOrder)) ? sequenceOrder : m.SequenceOrder),
                        ct);
            }
            return;
        }

        int updatedRows = await db.Messages
            .Where(m => m.ConversationId == conversationId && m.TurnId == turnId && m.Speaker == speaker)
            .ExecuteUpdateAsync(s => s
                .SetProperty(m => m.TranscriptLine1, fullTranscript)
                .SetProperty(m => m.IsFinal, true)
                .SetProperty(m => m.SequenceOrder, m => (sequenceOrder > 0 && (m.SequenceOrder == 0 || sequenceOrder < m.SequenceOrder)) ? sequenceOrder : m.SequenceOrder),
                ct);

        if (updatedRows > 0)
        {
            convEntity.Touch(_timeProvider.GetUtcNow());
            if (speaker == "user") convEntity.UpdateTitleFromFirstUserMessage(fullTranscript);
            await db.SaveChangesAsync(ct);
            return;
        }

        var finalMsg = ChatMessageEntity.CreateWithTranscript(
            conversationId: conversationId,
            turnId: turnId,
            speaker: speaker,
            sequenceOrder: sequenceOrder,
            fullTranscript: fullTranscript,
            isFinal: true);
        db.Messages.Add(finalMsg);
        convEntity.Touch(_timeProvider.GetUtcNow());
        if (speaker == "user") convEntity.UpdateTitleFromFirstUserMessage(fullTranscript);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            await using var retryDb = await _dbFactory.CreateDbContextAsync(ct);
            await retryDb.Messages
                .Where(m => m.ConversationId == conversationId && m.TurnId == turnId && m.Speaker == speaker)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(m => m.TranscriptLine1, fullTranscript)
                    .SetProperty(m => m.IsFinal, true)
                    .SetProperty(m => m.SequenceOrder, m => (sequenceOrder > 0 && (m.SequenceOrder == 0 || sequenceOrder < m.SequenceOrder)) ? sequenceOrder : m.SequenceOrder),
                    ct);
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

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        if (db.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            lock (_inMemoryLock)
            {
                var conv = db.Conversations.FirstOrDefault(c => c.Id == conversationId);
                if (conv is null) return;

                var msg = db.Messages.FirstOrDefault(
                    m => m.ConversationId == conversationId && m.TurnId == turnId && m.Speaker == speaker);

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
                db.SaveChanges();
                return;
            }
        }

        var convEntity = await db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, ct);
        if (convEntity is null)
            return;

        var existingMsg = await db.Messages.FirstOrDefaultAsync(
            m => m.ConversationId == conversationId && m.TurnId == turnId && m.Speaker == speaker,
            ct);

        if (existingMsg is null)
        {
            existingMsg = ChatMessageEntity.CreateWithTranslation(
                conversationId: conversationId,
                turnId: turnId,
                speaker: speaker,
                sequenceOrder: sequenceOrder,
                fullVietnameseText: fullVietnameseText);
            db.Messages.Add(existingMsg);
        }
        else
        {
            existingMsg.UpdateTranslation(fullVietnameseText, sequenceOrder);
        }

        convEntity.Touch(_timeProvider.GetUtcNow());

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            await using var retryDb = await _dbFactory.CreateDbContextAsync(ct);
            var retryConv = await retryDb.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, ct);
            var retryMsg = await retryDb.Messages.FirstOrDefaultAsync(
                m => m.ConversationId == conversationId && m.TurnId == turnId && m.Speaker == speaker,
                ct);

            if (retryMsg is not null)
            {
                retryMsg.UpdateTranslation(fullVietnameseText, sequenceOrder);
                retryConv?.Touch(_timeProvider.GetUtcNow());
                await retryDb.SaveChangesAsync(ct);
            }
        }
    }

    public async Task SaveCorrectionAsync(LanguageCorrectionEntity correction, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        bool exists = await db.Conversations.AnyAsync(c => c.Id == correction.ConversationId, ct);
        if (!exists)
            return;

        db.Corrections.Add(correction);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteConversationAsync(string userId, string id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var conv = await db.Conversations.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);
        if (conv is null)
            return false;

        conv.SoftDelete(_timeProvider.GetUtcNow(), userId);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
