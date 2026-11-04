using EnglishLiveTutor.Application.Modules.Lessons;
using EnglishLiveTutor.Domain.Lessons;
using EnglishLiveTutor.Domain.Lessons.Entities;
using EnglishLiveTutor.Domain.LessonAttempts;
using EnglishLiveTutor.Domain.LessonAttempts.Entities;
using EnglishLiveTutor.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Persistence.Modules;

public sealed class LessonsDbContext(DbContextOptions<LessonsDbContext> options)
    : DbContext(options), ILessonsDbContext
{
    public DbSet<LessonEntity> Lessons => Set<LessonEntity>();
    public DbSet<ExerciseEntity> Exercises => Set<ExerciseEntity>();
    public DbSet<LessonAttemptEntity> LessonAttempts => Set<LessonAttemptEntity>();
    public DbSet<ExerciseAnswerEntity> ExerciseAnswers => Set<ExerciseAnswerEntity>();
    public DbSet<LessonRolePlaySessionEntity> LessonRolePlaySessions => Set<LessonRolePlaySessionEntity>();
    public DbSet<LessonRolePlayTurnEntity> LessonRolePlayTurns => Set<LessonRolePlayTurnEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(ModuleSchemas.Lessons);
        modelBuilder.ApplyConfiguration(new LessonEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ExerciseEntityConfiguration());
        modelBuilder.ApplyConfiguration(new LessonAttemptEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ExerciseAnswerEntityConfiguration());
        modelBuilder.ApplyConfiguration(new LessonRolePlaySessionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new LessonRolePlayTurnEntityConfiguration());
    }
}
