using EnglishLiveTutor.Domain.AiCatalog;
using EnglishLiveTutor.Domain.Conversations;
using EnglishLiveTutor.Domain.Conversations.Entities;
using EnglishLiveTutor.Domain.Identity.Permissions;
using EnglishLiveTutor.Domain.Identity.Roles;
using EnglishLiveTutor.Domain.Identity.Roles.Entities;
using EnglishLiveTutor.Domain.Identity.Users;
using EnglishLiveTutor.Domain.Identity.Users.Entities;
using EnglishLiveTutor.Domain.Learning.DailyVocabularyPlans;
using EnglishLiveTutor.Domain.Learning.DailyVocabularyPlans.Entities;
using EnglishLiveTutor.Domain.Learning.ReviewAttempts;
using EnglishLiveTutor.Domain.Learning.UserVocabularyProgress;
using EnglishLiveTutor.Domain.Prompts.CefrLevelPrompts;
using EnglishLiveTutor.Domain.Prompts.ConversationModePrompts;
using EnglishLiveTutor.Domain.Prompts.SpeechSpeeds;
using EnglishLiveTutor.Domain.Pronunciation.Assessments;
using EnglishLiveTutor.Domain.Pronunciation.Assessments.Entities;
using EnglishLiveTutor.Domain.Pronunciation.AudioRecordings;
using EnglishLiveTutor.Domain.Pronunciation.GeneratedAudio;
using EnglishLiveTutor.Domain.Pronunciation.ScoringRubrics;
using EnglishLiveTutor.Domain.Pronunciation.ScoringRubrics.Entities;
using EnglishLiveTutor.Domain.Topics;
using EnglishLiveTutor.Domain.Vocabulary.GenerationRuns;
using EnglishLiveTutor.Domain.Vocabulary.Vocabularies;
using EnglishLiveTutor.Domain.Vocabulary.Vocabularies.Entities;
using EnglishLiveTutor.Domain.Voices;

namespace EnglishLiveTutor.Application.Common.Persistence;

/// <summary>
/// Read-only query surface owned by Application.
/// Exposes IQueryable sets without SaveChangesAsync or mutable DbSet operations.
/// </summary>
public interface IAppReadDbContext : IAsyncDisposable, IDisposable
{
    IQueryable<UserEntity> Users { get; }
    IQueryable<RoleEntity> Roles { get; }
    IQueryable<PermissionEntity> Permissions { get; }
    IQueryable<UserRoleEntity> UserRoles { get; }
    IQueryable<RolePermissionEntity> RolePermissions { get; }
    IQueryable<RefreshTokenEntity> RefreshTokens { get; }

    IQueryable<ConversationEntity> Conversations { get; }
    IQueryable<ChatMessageEntity> Messages { get; }
    IQueryable<LanguageCorrectionEntity> Corrections { get; }

    IQueryable<CefrLevelPromptEntity> CefrLevelPrompts { get; }
    IQueryable<ConversationModePromptEntity> ConversationModePrompts { get; }
    IQueryable<SpeechSpeedEntity> SpeechSpeeds { get; }

    IQueryable<TeacherVoiceEntity> TeacherVoices { get; }
    IQueryable<TopicEntity> Topics { get; }

    IQueryable<VocabularyEntity> Vocabularies { get; }
    IQueryable<VocabularySenseEntity> VocabularySenses { get; }
    IQueryable<VocabularyPronunciationEntity> VocabularyPronunciations { get; }
    IQueryable<VocabularyRelationEntity> VocabularyRelations { get; }
    IQueryable<VocabularyWordFamilyEntity> VocabularyWordFamilies { get; }
    IQueryable<VocabularyCollocationEntity> VocabularyCollocations { get; }
    IQueryable<VocabularyExampleEntity> VocabularyExamples { get; }
    IQueryable<VocabularyGrammarPatternEntity> VocabularyGrammarPatterns { get; }

    IQueryable<AiModelConfigEntity> AiModelCatalog { get; }
    IQueryable<VocabularyGenerationAuditEntity> VocabularyGenerationAudits { get; }

    IQueryable<DailyVocabularyPlanEntity> DailyVocabularyPlans { get; }
    IQueryable<DailyVocabularyItemEntity> DailyVocabularyItems { get; }
    IQueryable<UserVocabularyProgressEntity> UserVocabularyProgresses { get; }
    IQueryable<VocabularyReviewAttemptEntity> VocabularyReviewAttempts { get; }

    IQueryable<ScoringRubricEntity> ScoringRubrics { get; }
    IQueryable<ScoringRubricCriterionEntity> ScoringRubricCriteria { get; }
    IQueryable<AudioRecordingEntity> AudioRecordings { get; }
    IQueryable<PronunciationAssessmentEntity> PronunciationAssessments { get; }
    IQueryable<PronunciationCriterionScoreEntity> PronunciationCriterionScores { get; }
    IQueryable<GeneratedPronunciationAudioEntity> GeneratedPronunciationAudios { get; }
}

public interface IAppReadDbContextFactory
{
    Task<IAppReadDbContext> CreateReadDbContextAsync(CancellationToken ct = default);
}

public interface IAppDbContext : IAppReadDbContext
{
}

public interface IAppDbContextFactory : IAppReadDbContextFactory
{
    Task<IAppDbContext> CreateDbContextAsync(CancellationToken ct = default);
}
