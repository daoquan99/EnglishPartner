using EnglishLiveTutor.Application.Modules.Lessons;
using EnglishLiveTutor.Domain.Lessons;
using EnglishLiveTutor.Domain.Lessons.Repositories;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Repositories;

public sealed class LessonRolePlayRepository : ILessonRolePlayRepository
{
    private readonly ILessonsDbContext _db;

    public LessonRolePlayRepository(ILessonsDbContext db)
    {
        _db = db;
    }

    public Task<LessonRolePlaySessionEntity?> GetByIdAsync(Guid sessionId, CancellationToken ct = default)
    {
        return _db.LessonRolePlaySessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);
    }

    public Task<LessonRolePlaySessionEntity?> GetByIdWithTurnsAsync(Guid sessionId, CancellationToken ct = default)
    {
        return _db.LessonRolePlaySessions
            .Include(s => s.Turns)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);
    }

    public void Add(LessonRolePlaySessionEntity session)
    {
        _db.LessonRolePlaySessions.Add(session);
    }
}
