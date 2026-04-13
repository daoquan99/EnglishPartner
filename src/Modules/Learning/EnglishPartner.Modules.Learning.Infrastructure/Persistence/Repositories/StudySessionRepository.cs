using EnglishPartner.Modules.Learning.Domain.StudySessions;
using EnglishPartner.Modules.Learning.Domain.StudySessions.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Learning.Infrastructure.Persistence.Repositories;

internal sealed class StudySessionRepository(LearningDbContext dbContext) : IStudySessionRepository
{
    public async Task<StudySession?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.StudySessions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<List<StudySession>> GetUserSessionsAsync(Guid userId, CancellationToken cancellationToken)
        => await dbContext.StudySessions.Where(x => x.UserId == userId).OrderByDescending(x => x.StartTime).ToListAsync(cancellationToken);

    public async Task<int> AbandonStaleSessionsAsync(Guid userId, DateTime staleThreshold, CancellationToken cancellationToken)
    {
        var staleSessions = await dbContext.StudySessions
            .Where(x => x.UserId == userId && x.Status == SessionStatus.InProgress && x.LastActivityTime <= staleThreshold)
            .ToListAsync(cancellationToken);

        int count = 0;
        foreach (var session in staleSessions)
        {
            session.Abandon();
            count++;
        }
        return count;
    }

    public void Add(StudySession session) => dbContext.StudySessions.Add(session);
    public void Update(StudySession session) => dbContext.StudySessions.Update(session);
}
