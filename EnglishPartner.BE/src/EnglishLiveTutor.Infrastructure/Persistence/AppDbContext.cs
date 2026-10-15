using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.AiCatalog;
using EnglishLiveTutor.Domain.Common.Abstractions;
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

namespace EnglishLiveTutor.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Identity & Access Management Aggregate
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<RoleEntity> Roles => Set<RoleEntity>();
    public DbSet<PermissionEntity> Permissions => Set<PermissionEntity>();
    public DbSet<UserRoleEntity> UserRoles => Set<UserRoleEntity>();
    public DbSet<RolePermissionEntity> RolePermissions => Set<RolePermissionEntity>();
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();

    // Live Conversation Aggregate
    public DbSet<ConversationEntity> Conversations => Set<ConversationEntity>();
    public DbSet<ChatMessageEntity> Messages => Set<ChatMessageEntity>();
    public DbSet<LanguageCorrectionEntity> Corrections => Set<LanguageCorrectionEntity>();

    // Prompts & Coaching Modes (CefrPromptBuilder in DB)
    public DbSet<CefrLevelPromptEntity> CefrLevelPrompts => Set<CefrLevelPromptEntity>();
    public DbSet<ConversationModePromptEntity> ConversationModePrompts => Set<ConversationModePromptEntity>();
    public DbSet<SpeechSpeedEntity> SpeechSpeeds => Set<SpeechSpeedEntity>();

    // Teacher Voices Catalog
    public DbSet<TeacherVoiceEntity> TeacherVoices => Set<TeacherVoiceEntity>();

    // Topics Aggregate Catalog
    public DbSet<TopicEntity> Topics => Set<TopicEntity>();

    // Vocabulary Aggregate
    public DbSet<VocabularyEntity> Vocabularies => Set<VocabularyEntity>();
    public DbSet<VocabularySenseEntity> VocabularySenses => Set<VocabularySenseEntity>();
    public DbSet<VocabularyPronunciationEntity> VocabularyPronunciations => Set<VocabularyPronunciationEntity>();
    public DbSet<VocabularyRelationEntity> VocabularyRelations => Set<VocabularyRelationEntity>();
    public DbSet<VocabularyWordFamilyEntity> VocabularyWordFamilies => Set<VocabularyWordFamilyEntity>();
    public DbSet<VocabularyCollocationEntity> VocabularyCollocations => Set<VocabularyCollocationEntity>();
    public DbSet<VocabularyExampleEntity> VocabularyExamples => Set<VocabularyExampleEntity>();
    public DbSet<VocabularyGrammarPatternEntity> VocabularyGrammarPatterns => Set<VocabularyGrammarPatternEntity>();

    // AI Model Catalog & Audits
    public DbSet<AiModelConfigEntity> AiModelCatalog => Set<AiModelConfigEntity>();
    public DbSet<VocabularyGenerationAuditEntity> VocabularyGenerationAudits => Set<VocabularyGenerationAuditEntity>();

    // Daily Learning & Spaced Repetition Aggregate
    public DbSet<DailyVocabularyPlanEntity> DailyVocabularyPlans => Set<DailyVocabularyPlanEntity>();
    public DbSet<DailyVocabularyItemEntity> DailyVocabularyItems => Set<DailyVocabularyItemEntity>();
    public DbSet<UserVocabularyProgressEntity> UserVocabularyProgresses => Set<UserVocabularyProgressEntity>();
    public DbSet<VocabularyReviewAttemptEntity> VocabularyReviewAttempts => Set<VocabularyReviewAttemptEntity>();

    // Pronunciation Rubrics & Assessments
    public DbSet<ScoringRubricEntity> ScoringRubrics => Set<ScoringRubricEntity>();
    public DbSet<ScoringRubricCriterionEntity> ScoringRubricCriteria => Set<ScoringRubricCriterionEntity>();
    public DbSet<AudioRecordingEntity> AudioRecordings => Set<AudioRecordingEntity>();
    public DbSet<PronunciationAssessmentEntity> PronunciationAssessments => Set<PronunciationAssessmentEntity>();
    public DbSet<PronunciationCriterionScoreEntity> PronunciationCriterionScores => Set<PronunciationCriterionScoreEntity>();
    public DbSet<GeneratedPronunciationAudioEntity> GeneratedPronunciationAudios => Set<GeneratedPronunciationAudioEntity>();

    /// <summary>
    /// Explicit access to soft-deleted Aggregate Roots when authorized administrative or audit operations require it.
    /// </summary>
    public IQueryable<TEntity> QueryIncludingDeleted<TEntity>()
        where TEntity : class, ISoftDeletable =>
        Set<TEntity>().IgnoreQueryFilters();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

public sealed class AppDbContextFactoryWrapper : IAppDbContextFactory
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public AppDbContextFactoryWrapper(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<IAppDbContext> CreateDbContextAsync(CancellationToken ct = default)
    {
        return await _dbFactory.CreateDbContextAsync(ct);
    }
}
