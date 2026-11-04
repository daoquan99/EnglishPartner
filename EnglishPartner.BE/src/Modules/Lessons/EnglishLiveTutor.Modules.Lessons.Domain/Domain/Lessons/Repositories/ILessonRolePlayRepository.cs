namespace EnglishLiveTutor.Domain.Lessons.Repositories;

public interface ILessonRolePlayRepository
{
    Task<LessonRolePlaySessionEntity?> GetByIdAsync(Guid sessionId, CancellationToken ct = default);
    Task<LessonRolePlaySessionEntity?> GetByIdWithTurnsAsync(Guid sessionId, CancellationToken ct = default);
    void Add(LessonRolePlaySessionEntity session);
}
