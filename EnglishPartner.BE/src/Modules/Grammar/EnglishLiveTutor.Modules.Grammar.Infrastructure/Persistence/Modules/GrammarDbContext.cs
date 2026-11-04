using EnglishLiveTutor.Application.Modules.Grammar;
using EnglishLiveTutor.Domain.Grammar;
using EnglishLiveTutor.Domain.Grammar.Entities;
using EnglishLiveTutor.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Persistence.Modules;

public sealed class GrammarDbContext(DbContextOptions<GrammarDbContext> options)
    : DbContext(options), IGrammarDbContext
{
    public DbSet<GrammarStepEntity> GrammarSteps => Set<GrammarStepEntity>();
    public DbSet<GrammarExerciseEntity> GrammarExercises => Set<GrammarExerciseEntity>();
    public DbSet<UserGrammarStepProgressEntity> UserGrammarStepProgress => Set<UserGrammarStepProgressEntity>();
    public DbSet<GrammarAttemptEntity> GrammarAttempts => Set<GrammarAttemptEntity>();
    public DbSet<GrammarAttemptAnswerEntity> GrammarAttemptAnswers => Set<GrammarAttemptAnswerEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(ModuleSchemas.Grammar);
        modelBuilder.ApplyConfiguration(new GrammarStepEntityConfiguration());
        modelBuilder.ApplyConfiguration(new GrammarExerciseEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UserGrammarStepProgressEntityConfiguration());
        modelBuilder.ApplyConfiguration(new GrammarAttemptEntityConfiguration());
        modelBuilder.ApplyConfiguration(new GrammarAttemptAnswerEntityConfiguration());
    }
}
