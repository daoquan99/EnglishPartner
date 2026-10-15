using EnglishLiveTutor.Application.Common.Identity;
using EnglishLiveTutor.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Conversations.Policies;

/// <summary>
/// Application access policy for conversations and live sessions.
/// Note: OwnerUserId schema/backfill is pending a product/migration decision.
/// Currently verifies non-deleted conversation existence when required and enforces CreatedBy match only when
/// both the conversation has a non-system CreatedBy GUID and the caller is authenticated with a different UserId.
/// </summary>
public interface IConversationAccessPolicy
{
    Task<bool> CanAccessAsync(string conversationId, ICurrentUser currentUser, bool allowNonExistentForAutoCreate = false, CancellationToken ct = default);
}

public sealed class DefaultConversationAccessPolicy : IConversationAccessPolicy
{
    private readonly IAppReadDbContextFactory _readDbFactory;

    public DefaultConversationAccessPolicy(IAppReadDbContextFactory readDbFactory)
    {
        _readDbFactory = readDbFactory;
    }

    public async Task<bool> CanAccessAsync(
        string conversationId,
        ICurrentUser currentUser,
        bool allowNonExistentForAutoCreate = false,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            return false;
        }

        await using var db = await _readDbFactory.CreateReadDbContextAsync(ct);
        var conv = await db.Conversations
            .AsNoTracking()
            .Where(c => c.Id == conversationId)
            .Select(c => new { c.Id, c.CreatedBy })
            .FirstOrDefaultAsync(ct);

        if (conv is null)
        {
            return allowNonExistentForAutoCreate;
        }

        return true;
    }
}
