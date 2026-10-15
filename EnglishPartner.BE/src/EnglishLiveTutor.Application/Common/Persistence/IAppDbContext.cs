using EnglishLiveTutor.Domain.AiCatalog;
using EnglishLiveTutor.Domain.Conversations.Conversations;
using EnglishLiveTutor.Domain.Conversations.Conversations.Entities;
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
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Common.Persistence;

public interface IAppDbContext : IApplicationReadDbContext, IUnitOfWork
{
    new DbSet<UserEntity> Users { get; }
    new DbSet<RoleEntity> Roles { get; }
    new DbSet<PermissionEntity> Permissions { get; }
    new DbSet<UserRoleEntity> UserRoles { get; }
    new DbSet<RolePermissionEntity> RolePermissions { get; }
    new DbSet<RefreshTokenEntity> RefreshTokens { get; }

    new DbSet<ConversationEntity> Conversations { get; }
    new DbSet<ChatMessageEntity> Messages { get; }
    new DbSet<LanguageCorrectionEntity> Corrections { get; }

    new DbSet<CefrLevelPromptEntity> CefrLevelPrompts { get; }
    new DbSet<ConversationModePromptEntity> ConversationModePrompts { get; }
    new DbSet<SpeechSpeedEntity> SpeechSpeeds { get; }

    new DbSet<TeacherVoiceEntity> TeacherVoices { get; }
    new DbSet<TopicEntity> Topics { get; }

    new DbSet<VocabularyEntity> Vocabularies { get; }
    new DbSet<VocabularySenseEntity> VocabularySenses { get; }
    new DbSet<VocabularyPronunciationEntity> VocabularyPronunciations { get; }
    new DbSet<VocabularyRelationEntity> VocabularyRelations { get; }
    new DbSet<VocabularyWordFamilyEntity> VocabularyWordFamilies { get; }
    new DbSet<VocabularyCollocationEntity> VocabularyCollocations { get; }
    new DbSet<VocabularyExampleEntity> VocabularyExamples { get; }
    new DbSet<VocabularyGrammarPatternEntity> VocabularyGrammarPatterns { get; }

    new DbSet<AiModelConfigEntity> AiModelCatalog { get; }
    new DbSet<VocabularyGenerationAuditEntity> VocabularyGenerationAudits { get; }

    new DbSet<DailyVocabularyPlanEntity> DailyVocabularyPlans { get; }
    new DbSet<DailyVocabularyItemEntity> DailyVocabularyItems { get; }
    new DbSet<UserVocabularyProgressEntity> UserVocabularyProgresses { get; }
    new DbSet<VocabularyReviewAttemptEntity> VocabularyReviewAttempts { get; }

    new DbSet<ScoringRubricEntity> ScoringRubrics { get; }
    new DbSet<ScoringRubricCriterionEntity> ScoringRubricCriteria { get; }
    new DbSet<AudioRecordingEntity> AudioRecordings { get; }
    new DbSet<PronunciationAssessmentEntity> PronunciationAssessments { get; }
    new DbSet<PronunciationCriterionScoreEntity> PronunciationCriterionScores { get; }
    new DbSet<GeneratedPronunciationAudioEntity> GeneratedPronunciationAudios { get; }
}

public interface IAppDbContextFactory : IApplicationReadDbContextFactory
{
    Task<IAppDbContext> CreateDbContextAsync(CancellationToken ct = default);

    async Task<IApplicationReadDbContext> IApplicationReadDbContextFactory.CreateReadContextAsync(CancellationToken ct) =>
        await CreateDbContextAsync(ct);
}
