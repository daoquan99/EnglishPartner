using EnglishPartner.Modules.Learning.Domain.StudySessions;
using EnglishPartner.Modules.Learning.Domain.StudySessions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Learning.Infrastructure.Persistence.Repositories;

internal sealed class StudySessionRepository(LearningDbContext dbContext, TimeProvider timeProvider) : IStudySessionRepository
{
    public async Task<StudySession?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.StudySessions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<List<StudySession>> GetUserSessionsAsync(Guid userId, CancellationToken cancellationToken)
        => await dbContext.StudySessions
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.StartTime)
            .Take(50)   // Cap: frontend paginates; keep most recent 50
            .ToListAsync(cancellationToken);

    public async Task<int> AbandonStaleSessionsAsync(Guid userId, DateTime staleThreshold, CancellationToken cancellationToken)
    {
        // Bulk UPDATE in one SQL statement — avoids load-then-loop N+1 pattern
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        return await dbContext.StudySessions
            .Where(x => x.UserId == userId
                     && x.Status == SessionStatus.InProgress
                     && x.LastActivityTime <= staleThreshold)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, SessionStatus.Abandoned)
                .SetProperty(x => x.EndTime, utcNow),
                cancellationToken);
    }

    public void Add(StudySession session) => dbContext.StudySessions.Add(session);
    public void Update(StudySession session) => dbContext.StudySessions.Update(session);
}
