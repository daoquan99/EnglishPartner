using EnglishLiveTutor.Domain.Conversations;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Persistence.Repositories.Conversations;

public sealed class ConversationRepository : IConversationRepository
{
    private readonly AppDbContext _db;

    public ConversationRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<ConversationEntity?> GetByIdAsync(string id, CancellationToken ct = default) =>
        _db.Conversations.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<ConversationEntity?> GetWithHistoryAsync(string id, CancellationToken ct = default)
    {
        return await _db.Conversations
            .Include(c => c.Messages
                .Where(m => !m.IsDeleted && (m.TranscriptLine1 != "" || m.TranslationLine2 != ""))
                .OrderBy(m => m.SequenceOrder))
            .Include(c => c.Corrections
                .Where(corr => !corr.IsDeleted)
                .OrderByDescending(x => x.CreatedAtUtc))
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public Task AddAsync(ConversationEntity conversation, CancellationToken ct = default)
    {
        _db.Conversations.Add(conversation);
        return Task.CompletedTask;
    }
}
