using EnglishLiveTutor.Application.Modules.Vocabulary;
using EnglishLiveTutor.Domain.Learning;
using EnglishLiveTutor.Domain.Learning.Entities;
using EnglishLiveTutor.Domain.Vocabulary;
using EnglishLiveTutor.Domain.Vocabulary.Entities;
using EnglishLiveTutor.Domain.VocabularyGames;
using EnglishLiveTutor.Domain.VocabularyGames.Entities;
using EnglishLiveTutor.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Persistence.Modules;

public sealed class VocabularyDbContext(DbContextOptions<VocabularyDbContext> options)
    : DbContext(options), IVocabularyDbContext
{
    public DbSet<VocabularyEntity> Vocabularies => Set<VocabularyEntity>();
    public DbSet<VocabularySenseEntity> VocabularySenses => Set<VocabularySenseEntity>();
    public DbSet<VocabularyPronunciationEntity> VocabularyPronunciations => Set<VocabularyPronunciationEntity>();
    public DbSet<VocabularyRelationEntity> VocabularyRelations => Set<VocabularyRelationEntity>();
    public DbSet<VocabularyWordFamilyEntity> VocabularyWordFamilies => Set<VocabularyWordFamilyEntity>();
    public DbSet<VocabularyCollocationEntity> VocabularyCollocations => Set<VocabularyCollocationEntity>();
    public DbSet<VocabularyExampleEntity> VocabularyExamples => Set<VocabularyExampleEntity>();
    public DbSet<VocabularyGrammarPatternEntity> VocabularyGrammarPatterns => Set<VocabularyGrammarPatternEntity>();
    public DbSet<VocabularyGenerationAuditEntity> VocabularyGenerationAudits => Set<VocabularyGenerationAuditEntity>();
    public DbSet<UserLearningSettingsEntity> UserLearningSettings => Set<UserLearningSettingsEntity>();
    public DbSet<DailyVocabularyPlanEntity> DailyVocabularyPlans => Set<DailyVocabularyPlanEntity>();
    public DbSet<DailyVocabularyItemEntity> DailyVocabularyItems => Set<DailyVocabularyItemEntity>();
    public DbSet<UserVocabularyProgressEntity> UserVocabularyProgresses => Set<UserVocabularyProgressEntity>();
    public DbSet<VocabularyReviewAttemptEntity> VocabularyReviewAttempts => Set<VocabularyReviewAttemptEntity>();
    public DbSet<FallingWordGameSessionEntity> FallingWordGameSessions => Set<FallingWordGameSessionEntity>();
    public DbSet<FallingWordGameRoundEntity> FallingWordGameRounds => Set<FallingWordGameRoundEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(ModuleSchemas.Vocabulary);
        modelBuilder.ApplyConfiguration(new VocabularyEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VocabularySenseEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VocabularyPronunciationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VocabularyRelationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VocabularyWordFamilyEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VocabularyCollocationEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VocabularyExampleEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VocabularyGrammarPatternEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VocabularyGenerationAuditEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UserLearningSettingsEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DailyVocabularyPlanEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DailyVocabularyItemEntityConfiguration());
        modelBuilder.ApplyConfiguration(new UserVocabularyProgressEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VocabularyReviewAttemptEntityConfiguration());
        modelBuilder.ApplyConfiguration(new FallingWordGameSessionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new FallingWordGameRoundEntityConfiguration());
    }
}
