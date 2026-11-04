using EnglishLiveTutor.Domain.Learning;
using EnglishLiveTutor.Domain.Learning.Entities;
using EnglishLiveTutor.Domain.Vocabulary;
using EnglishLiveTutor.Domain.Vocabulary.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Modules.Vocabulary;

public interface IVocabularyDbContext : IModuleDbContext
{
    DbSet<VocabularyEntity> Vocabularies { get; }
    DbSet<VocabularySenseEntity> VocabularySenses { get; }
    DbSet<VocabularyPronunciationEntity> VocabularyPronunciations { get; }
    DbSet<VocabularyRelationEntity> VocabularyRelations { get; }
    DbSet<VocabularyWordFamilyEntity> VocabularyWordFamilies { get; }
    DbSet<VocabularyCollocationEntity> VocabularyCollocations { get; }
    DbSet<VocabularyExampleEntity> VocabularyExamples { get; }
    DbSet<VocabularyGrammarPatternEntity> VocabularyGrammarPatterns { get; }
    DbSet<VocabularyGenerationAuditEntity> VocabularyGenerationAudits { get; }
    DbSet<UserLearningSettingsEntity> UserLearningSettings { get; }
    DbSet<DailyVocabularyPlanEntity> DailyVocabularyPlans { get; }
    DbSet<DailyVocabularyItemEntity> DailyVocabularyItems { get; }
    DbSet<UserVocabularyProgressEntity> UserVocabularyProgresses { get; }
    DbSet<VocabularyReviewAttemptEntity> VocabularyReviewAttempts { get; }
    DbSet<EnglishLiveTutor.Domain.VocabularyGames.FallingWordGameSessionEntity> FallingWordGameSessions { get; }
    DbSet<EnglishLiveTutor.Domain.VocabularyGames.Entities.FallingWordGameRoundEntity> FallingWordGameRounds { get; }
}
