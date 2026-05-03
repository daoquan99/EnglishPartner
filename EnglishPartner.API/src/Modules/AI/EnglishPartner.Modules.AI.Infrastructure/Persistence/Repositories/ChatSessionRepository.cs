using EnglishPartner.Modules.AI.Domain.ChatSessions;
using EnglishPartner.Modules.AI.Domain.ChatSessions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.AI.Infrastructure.Persistence.Repositories;

internal sealed class ChatSessionRepository(AiDbContext context) : IChatSessionRepository
{
    public async Task<ChatSession?> GetByIdWithMessagesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.ChatSessions
            .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<List<ChatSession>> GetUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.ChatSessions
            .Include(s => s.Messages)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Lightweight projection for list view — counts messages in SQL, avoids loading entities.
    /// </summary>
    public async Task<List<ChatSessionSummary>> GetUserSessionSummariesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.ChatSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new ChatSessionSummary(
                s.Id,
                s.Title,
                s.SystemPrompt,
                s.Topic,
                s.SessionType,
                s.VoiceName,
                s.Messages.Count,   // Translated to COUNT(*) subquery in SQL
                s.CreatedAt,
                s.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ChatHistorySummary?> GetLatestSummaryAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await context.Set<ChatHistorySummary>()
            .AsNoTracking()
            .Where(s => s.SessionId == sessionId)
            .OrderByDescending(s => s.EndOrder)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void Add(ChatSession session) => context.ChatSessions.Add(session);
    public void Update(ChatSession session) => context.ChatSessions.Update(session);
}
