using EnglishLiveTutor.Domain.AiCatalog;
using EnglishLiveTutor.Domain.Conversations;
using EnglishLiveTutor.Domain.Conversations.Entities;
using EnglishLiveTutor.Domain.Identity;
using EnglishLiveTutor.Domain.Identity.Entities;
using EnglishLiveTutor.Domain.Learning;
using EnglishLiveTutor.Domain.Learning.Entities;
using EnglishLiveTutor.Domain.Prompts;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Pronunciation.Entities;
using EnglishLiveTutor.Domain.Vocabulary;
using EnglishLiveTutor.Domain.Vocabulary.Entities;
using EnglishLiveTutor.Domain.Topics;
using EnglishLiveTutor.Domain.Voices;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Common.Interfaces;

public interface IAppDbContext : IAsyncDisposable, IDisposable
{
    // Identity & Access Management
    DbSet<UserEntity> Users { get; }
    DbSet<RoleEntity> Roles { get; }
    DbSet<PermissionEntity> Permissions { get; }
    DbSet<UserRoleEntity> UserRoles { get; }
    DbSet<RolePermissionEntity> RolePermissions { get; }
    DbSet<RefreshTokenEntity> RefreshTokens { get; }

    // Live Conversations
    DbSet<ConversationEntity> Conversations { get; }
    DbSet<ChatMessageEntity> Messages { get; }
    DbSet<LanguageCorrectionEntity> Corrections { get; }

    // Prompts & Coaching Modes (CefrPromptBuilder in DB)
    DbSet<CefrLevelPromptEntity> CefrLevelPrompts { get; }
    DbSet<ConversationModePromptEntity> ConversationModePrompts { get; }
    DbSet<SpeechSpeedEntity> SpeechSpeeds { get; }

    // Teacher Voices Catalog
    DbSet<TeacherVoiceEntity> TeacherVoices { get; }

    // Topics Catalog Aggregate
    DbSet<TopicEntity> Topics { get; }

    // Vocabulary Aggregate
    DbSet<VocabularyEntity> Vocabularies { get; }
    DbSet<VocabularySenseEntity> VocabularySenses { get; }
    DbSet<VocabularyPronunciationEntity> VocabularyPronunciations { get; }
    DbSet<VocabularyRelationEntity> VocabularyRelations { get; }
    DbSet<VocabularyWordFamilyEntity> VocabularyWordFamilies { get; }
    DbSet<VocabularyCollocationEntity> VocabularyCollocations { get; }
    DbSet<VocabularyExampleEntity> VocabularyExamples { get; }
    DbSet<VocabularyGrammarPatternEntity> VocabularyGrammarPatterns { get; }

    // AI Model Catalog & Audits
    DbSet<AiModelConfigEntity> AiModelCatalog { get; }
    DbSet<VocabularyGenerationAuditEntity> VocabularyGenerationAudits { get; }

    // Daily Learning & Spaced Repetition
    DbSet<DailyVocabularyPlanEntity> DailyVocabularyPlans { get; }
    DbSet<DailyVocabularyItemEntity> DailyVocabularyItems { get; }
    DbSet<UserVocabularyProgressEntity> UserVocabularyProgresses { get; }
    DbSet<VocabularyReviewAttemptEntity> VocabularyReviewAttempts { get; }

    // Pronunciation Rubrics & Assessments
    DbSet<ScoringRubricEntity> ScoringRubrics { get; }
    DbSet<ScoringRubricCriterionEntity> ScoringRubricCriteria { get; }
    DbSet<AudioRecordingEntity> AudioRecordings { get; }
    DbSet<PronunciationAssessmentEntity> PronunciationAssessments { get; }
    DbSet<PronunciationCriterionScoreEntity> PronunciationCriterionScores { get; }
    DbSet<GeneratedPronunciationAudioEntity> GeneratedPronunciationAudios { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IAppDbContextFactory
{
    Task<IAppDbContext> CreateDbContextAsync(CancellationToken ct = default);
}
