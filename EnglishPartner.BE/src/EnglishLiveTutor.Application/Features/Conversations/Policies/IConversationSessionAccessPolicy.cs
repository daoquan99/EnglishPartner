using EnglishLiveTutor.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Conversations.Policies;

/// <summary>
/// Validates conversation access and session parameters before SignalR group join, session initialization, streaming, or shutdown.
/// Note: Persistent OwnerUserId column/backfill on Conversations remains a pending schema decision (see docs/backend-refactor-execution.md);
/// this policy currently validates non-empty conversation identity, checks parent visibility (!IsDeleted) when existing,
/// and enforces connection-to-conversation registration boundaries.
/// </summary>
public interface IConversationSessionAccessPolicy
{
    Task EnsureCanInitializeSessionAsync(
        string conversationId,
        string? actorUserId,
        bool createIfMissing = false,
        CancellationToken ct = default);

    void ValidateConversationId(string conversationId);
}

public sealed class ConversationSessionAccessPolicy(IApplicationReadDbContextFactory readDbFactory) : IConversationSessionAccessPolicy
{
    public void ValidateConversationId(string conversationId)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            throw new InvalidOperationException("ConversationId không được để trống.");
        }
    }

    public async Task EnsureCanInitializeSessionAsync(
        string conversationId,
        string? actorUserId,
        bool createIfMissing = false,
        CancellationToken ct = default)
    {
        ValidateConversationId(conversationId);
        string cleanId = conversationId.Trim();

        await using var db = await readDbFactory.CreateReadContextAsync(ct);
        var existing = await db.Conversations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == cleanId, ct);

        if (existing is not null && existing.IsDeleted)
        {
            throw new InvalidOperationException("Cuộc hội thoại đã bị xóa và không thể mở phiên trực tiếp.");
        }
    }
}
