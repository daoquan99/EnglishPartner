namespace EnglishPartner.Modules.Learning.Domain.StudySessions.Repositories;

public interface IStudySessionRepository
{
    Task<StudySession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<StudySession>> GetUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> AbandonStaleSessionsAsync(Guid userId, DateTime staleThreshold, CancellationToken cancellationToken = default);
    void Add(StudySession session);
    void Update(StudySession session);
}
