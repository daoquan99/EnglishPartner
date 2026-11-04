using EnglishLiveTutor.Domain.Lessons;
using EnglishLiveTutor.Domain.Lessons.Entities;
using EnglishLiveTutor.Domain.LessonAttempts;
using EnglishLiveTutor.Domain.LessonAttempts.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Modules.Lessons;

public interface ILessonsDbContext : IModuleDbContext
{
    DbSet<LessonEntity> Lessons { get; }
    DbSet<ExerciseEntity> Exercises { get; }
    DbSet<LessonAttemptEntity> LessonAttempts { get; }
    DbSet<ExerciseAnswerEntity> ExerciseAnswers { get; }
    DbSet<LessonRolePlaySessionEntity> LessonRolePlaySessions { get; }
    DbSet<LessonRolePlayTurnEntity> LessonRolePlayTurns { get; }
}
