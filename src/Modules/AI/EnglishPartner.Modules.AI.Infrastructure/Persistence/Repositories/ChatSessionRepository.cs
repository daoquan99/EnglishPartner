using EnglishPartner.Modules.AI.Domain.ChatSessions;
using EnglishPartner.Modules.AI.Domain.ChatSessions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.AI.Infrastructure.Persistence.Repositories;

internal sealed class ChatSessionRepository(AiDbContext context) : IChatSessionRepository
{
    public async Task<ChatSession?> GetByIdWithMessagesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.ChatSessions
            .Include(s => s.Messages.OrderBy(m => m.Timestamp))
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

    public void Add(ChatSession session) => context.ChatSessions.Add(session);
    public void Update(ChatSession session) => context.ChatSessions.Update(session);
}
