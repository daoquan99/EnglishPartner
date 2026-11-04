using EnglishLiveTutor.Infrastructure.Persistence;
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLiveTutor.Infrastructure.Persistence.Modules.Migrations.Vocabulary
{
    /// <inheritdoc />
    public partial class MoveToModuleSchema_Vocabulary : Migration
    {
        /// <inheritdoc />
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "vocabulary");

            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildGuardLegacySql(
                "vocabulary",
                [
                    "DailyVocabularyPlans",
                    "FallingWordGameSessions",
                    "UserLearningSettings",
                    "UserVocabularyProgresses",
                    "Vocabularies",
                    "VocabularyGenerationAudits",
                    "VocabularyReviewAttempts",
                    "DailyVocabularyItems",
                    "FallingWordGameRounds",
                    "VocabularyPronunciations",
                    "VocabularySenses",
                    "VocabularyWordFamilies",
                    "VocabularyCollocations",
                    "VocabularyExamples",
                    "VocabularyGrammarPatterns",
                    "VocabularyRelations"
                ]));

            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("vocabulary", "DailyVocabularyPlans"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("vocabulary", "FallingWordGameSessions"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("vocabulary", "UserLearningSettings"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("vocabulary", "UserVocabularyProgresses"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("vocabulary", "Vocabularies"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("vocabulary", "VocabularyGenerationAudits"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("vocabulary", "VocabularyReviewAttempts"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("vocabulary", "DailyVocabularyItems"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("vocabulary", "FallingWordGameRounds"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("vocabulary", "VocabularyPronunciations"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("vocabulary", "VocabularySenses"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("vocabulary", "VocabularyWordFamilies"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("vocabulary", "VocabularyCollocations"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("vocabulary", "VocabularyExamples"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("vocabulary", "VocabularyGrammarPatterns"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("vocabulary", "VocabularyRelations"));

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[vocabulary].[DailyVocabularyPlans]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [vocabulary].[DailyVocabularyPlans] (
                    [Id] uniqueidentifier NOT NULL,
                    [UserId] nvarchar(64) NOT NULL,
                    [LearningDate] nvarchar(16) NOT NULL,
                    [TimeZoneId] nvarchar(64) NOT NULL DEFAULT N''Asia/Ho_Chi_Minh'',
                    [NewVocabularyTarget] int NOT NULL,
                    [TargetCefrLevel] nvarchar(8) NULL,
                    [Status] nvarchar(32) NOT NULL DEFAULT N''Active'',
                    [CompletedAt] datetimeoffset NULL,
                    [CreatedAt] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset NULL,
                    [DeletedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_DailyVocabularyPlans] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[vocabulary].[FallingWordGameSessions]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [vocabulary].[FallingWordGameSessions] (
                    [Id] uniqueidentifier NOT NULL,
                    [UserId] nvarchar(64) NOT NULL,
                    [Source] nvarchar(32) NOT NULL,
                    [CefrLevel] nvarchar(8) NULL,
                    [PlayFilter] nvarchar(32) NOT NULL,
                    [DailyPlanId] uniqueidentifier NULL,
                    [LearningDate] nvarchar(16) NULL,
                    [TimeZoneId] nvarchar(64) NULL,
                    [TimeLimitSeconds] int NOT NULL,
                    [RequestedCount] int NOT NULL,
                    [ActualCount] int NOT NULL,
                    [Status] nvarchar(32) NOT NULL,
                    [StartedAtUtc] datetimeoffset NULL,
                    [CompletedAtUtc] datetimeoffset NULL,
                    [CorrectCount] int NOT NULL,
                    [IncorrectCount] int NOT NULL,
                    [TimedOutCount] int NOT NULL,
                    [AbandonedCount] int NOT NULL,
                    [BestStreak] int NOT NULL,
                    [CurrentStreak] int NOT NULL,
                    [RowVersion] rowversion NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset NULL,
                    [DeletedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_FallingWordGameSessions] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[vocabulary].[UserLearningSettings]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [vocabulary].[UserLearningSettings] (
                    [Id] uniqueidentifier NOT NULL,
                    [UserId] nvarchar(64) NOT NULL,
                    [DailyVocabularyTarget] int NOT NULL,
                    [CefrLevel] nvarchar(8) NOT NULL DEFAULT N''B1'',
                    [RowVersion] rowversion NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset NULL,
                    [DeletedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_UserLearningSettings] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[vocabulary].[UserVocabularyProgresses]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [vocabulary].[UserVocabularyProgresses] (
                    [Id] uniqueidentifier NOT NULL,
                    [UserId] nvarchar(64) NOT NULL,
                    [VocabularyId] uniqueidentifier NOT NULL,
                    [SenseId] uniqueidentifier NOT NULL,
                    [MasteryLevel] int NOT NULL,
                    [CorrectCount] int NOT NULL,
                    [IncorrectCount] int NOT NULL,
                    [ReviewCount] int NOT NULL,
                    [LastReviewedAt] datetimeoffset NULL,
                    [NextReviewAt] datetimeoffset NULL,
                    [IntervalDays] int NOT NULL,
                    [EaseFactor] decimal(5,2) NOT NULL DEFAULT 2.5,
                    [LearningStatus] nvarchar(32) NOT NULL DEFAULT N''New'',
                    [LatestPronunciationScore] decimal(5,2) NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset NULL,
                    [DeletedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_UserVocabularyProgresses] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[vocabulary].[Vocabularies]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [vocabulary].[Vocabularies] (
                    [Id] uniqueidentifier NOT NULL,
                    [Word] nvarchar(160) NOT NULL,
                    [NormalizedWord] nvarchar(160) NOT NULL,
                    [LanguageCode] nvarchar(16) NOT NULL DEFAULT N''en'',
                    [FrequencyRank] int NULL,
                    [SourceType] nvarchar(32) NOT NULL DEFAULT N''Seeded'',
                    [SourceModelId] nvarchar(64) NULL,
                    [TopicCategory] nvarchar(128) NULL,
                    [IsActive] bit NOT NULL,
                    [CreatedAt] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAt] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset NULL,
                    [DeletedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_Vocabularies] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[vocabulary].[VocabularyGenerationAudits]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [vocabulary].[VocabularyGenerationAudits] (
                    [Id] uniqueidentifier NOT NULL,
                    [UserId] nvarchar(64) NOT NULL,
                    [ModelCode] nvarchar(128) NOT NULL,
                    [PromptVersion] nvarchar(64) NOT NULL,
                    [RequestedCount] int NOT NULL,
                    [AcceptedCount] int NOT NULL,
                    [RejectedCount] int NOT NULL,
                    [Status] nvarchar(32) NOT NULL,
                    [ErrorMessage] nvarchar(max) NULL,
                    [CreatedAt] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset NULL,
                    [DeletedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_VocabularyGenerationAudits] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[vocabulary].[VocabularyReviewAttempts]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [vocabulary].[VocabularyReviewAttempts] (
                    [Id] uniqueidentifier NOT NULL,
                    [UserId] nvarchar(64) NOT NULL,
                    [VocabularyId] uniqueidentifier NOT NULL,
                    [SenseId] uniqueidentifier NOT NULL,
                    [ReviewType] nvarchar(32) NOT NULL,
                    [Rating] nvarchar(16) NOT NULL,
                    [ReviewedAt] datetimeoffset NOT NULL,
                    [NextReviewAtSnapshot] datetimeoffset NULL,
                    [ClientRequestId] nvarchar(64) NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset NULL,
                    [DeletedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_VocabularyReviewAttempts] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[vocabulary].[DailyVocabularyItems]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [vocabulary].[DailyVocabularyItems] (
                    [Id] uniqueidentifier NOT NULL,
                    [PlanId] uniqueidentifier NOT NULL,
                    [VocabularyId] uniqueidentifier NOT NULL,
                    [SenseId] uniqueidentifier NOT NULL,
                    [ItemType] nvarchar(32) NOT NULL,
                    [Status] nvarchar(32) NOT NULL,
                    [AssignedAt] datetimeoffset NOT NULL,
                    [CompletedAt] datetimeoffset NULL,
                    [DisplayOrder] int NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_DailyVocabularyItems] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[vocabulary].[FallingWordGameRounds]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [vocabulary].[FallingWordGameRounds] (
                    [Id] uniqueidentifier NOT NULL,
                    [SessionId] uniqueidentifier NOT NULL,
                    [RoundOrder] int NOT NULL,
                    [VocabularyId] uniqueidentifier NOT NULL,
                    [SenseId] uniqueidentifier NOT NULL,
                    [DailyItemId] uniqueidentifier NULL,
                    [TargetWordSnapshot] nvarchar(256) NOT NULL,
                    [NormalizedWordSnapshot] nvarchar(256) NOT NULL,
                    [MeaningViSnapshot] nvarchar(1000) NOT NULL,
                    [CefrLevelSnapshot] nvarchar(8) NULL,
                    [MaskedPattern] nvarchar(500) NOT NULL,
                    [LetterCount] int NOT NULL,
                    [Status] nvarchar(32) NOT NULL,
                    [StartedAtUtc] datetimeoffset NULL,
                    [ExpiresAtUtc] datetimeoffset NULL,
                    [SubmittedAtUtc] datetimeoffset NULL,
                    [SubmittedText] nvarchar(256) NULL,
                    [ElapsedMs] bigint NULL,
                    [FailureReason] nvarchar(64) NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_FallingWordGameRounds] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[vocabulary].[VocabularyPronunciations]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [vocabulary].[VocabularyPronunciations] (
                    [Id] uniqueidentifier NOT NULL,
                    [VocabularyId] uniqueidentifier NOT NULL,
                    [Accent] nvarchar(16) NOT NULL DEFAULT N''US'',
                    [Ipa] nvarchar(160) NOT NULL,
                    [AudioUrl] nvarchar(512) NULL,
                    [AudioSource] nvarchar(64) NOT NULL DEFAULT N''BrowserTTS'',
                    [SyllableCount] int NULL,
                    [StressPattern] nvarchar(64) NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_VocabularyPronunciations] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[vocabulary].[VocabularySenses]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [vocabulary].[VocabularySenses] (
                    [Id] uniqueidentifier NOT NULL,
                    [VocabularyId] uniqueidentifier NOT NULL,
                    [PartOfSpeech] nvarchar(64) NOT NULL,
                    [DefinitionEn] nvarchar(1000) NOT NULL,
                    [MeaningVi] nvarchar(1000) NOT NULL,
                    [DescriptionEn] nvarchar(1500) NULL,
                    [CefrLevel] nvarchar(8) NOT NULL DEFAULT N''B1'',
                    [IsPrimary] bit NOT NULL,
                    [DisplayOrder] int NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_VocabularySenses] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[vocabulary].[VocabularyWordFamilies]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [vocabulary].[VocabularyWordFamilies] (
                    [Id] uniqueidentifier NOT NULL,
                    [VocabularyId] uniqueidentifier NOT NULL,
                    [RelatedWord] nvarchar(160) NOT NULL,
                    [PartOfSpeech] nvarchar(64) NOT NULL,
                    [RelatedVocabularyId] uniqueidentifier NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_VocabularyWordFamilies] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[vocabulary].[VocabularyCollocations]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [vocabulary].[VocabularyCollocations] (
                    [Id] uniqueidentifier NOT NULL,
                    [SenseId] uniqueidentifier NOT NULL,
                    [PhraseEn] nvarchar(300) NOT NULL,
                    [MeaningVi] nvarchar(300) NULL,
                    [DisplayOrder] int NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_VocabularyCollocations] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[vocabulary].[VocabularyExamples]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [vocabulary].[VocabularyExamples] (
                    [Id] uniqueidentifier NOT NULL,
                    [SenseId] uniqueidentifier NOT NULL,
                    [SentenceEn] nvarchar(600) NOT NULL,
                    [SentenceVi] nvarchar(600) NULL,
                    [AudioUrl] nvarchar(512) NULL,
                    [CefrLevel] nvarchar(8) NULL,
                    [DisplayOrder] int NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_VocabularyExamples] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[vocabulary].[VocabularyGrammarPatterns]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [vocabulary].[VocabularyGrammarPatterns] (
                    [Id] uniqueidentifier NOT NULL,
                    [SenseId] uniqueidentifier NOT NULL,
                    [Pattern] nvarchar(300) NOT NULL,
                    [Explanation] nvarchar(600) NULL,
                    [Example] nvarchar(600) NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_VocabularyGrammarPatterns] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[vocabulary].[VocabularyRelations]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [vocabulary].[VocabularyRelations] (
                    [Id] uniqueidentifier NOT NULL,
                    [SenseId] uniqueidentifier NOT NULL,
                    [RelationType] nvarchar(32) NOT NULL,
                    [RelatedWord] nvarchar(160) NOT NULL,
                    [RelatedVocabularyId] uniqueidentifier NULL,
                    [RelatedSenseId] uniqueidentifier NULL,
                    [DisplayOrder] int NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_VocabularyRelations] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_DailyVocabularyItems_PlanId_SenseId' AND [object_id] = OBJECT_ID(N'[vocabulary].[DailyVocabularyItems]')) EXEC(N'CREATE UNIQUE INDEX [IX_DailyVocabularyItems_PlanId_SenseId] ON [vocabulary].[DailyVocabularyItems] ([PlanId], [SenseId]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_DailyVocabularyItems_PlanId_VocabularyId' AND [object_id] = OBJECT_ID(N'[vocabulary].[DailyVocabularyItems]')) EXEC(N'CREATE INDEX [IX_DailyVocabularyItems_PlanId_VocabularyId] ON [vocabulary].[DailyVocabularyItems] ([PlanId], [VocabularyId]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_DailyVocabularyPlans_UserId_LearningDate' AND [object_id] = OBJECT_ID(N'[vocabulary].[DailyVocabularyPlans]')) EXEC(N'CREATE UNIQUE INDEX [IX_DailyVocabularyPlans_UserId_LearningDate] ON [vocabulary].[DailyVocabularyPlans] ([UserId], [LearningDate]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_FallingWordGameRounds_SenseId_Status' AND [object_id] = OBJECT_ID(N'[vocabulary].[FallingWordGameRounds]')) EXEC(N'CREATE INDEX [IX_FallingWordGameRounds_SenseId_Status] ON [vocabulary].[FallingWordGameRounds] ([SenseId], [Status]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_FallingWordGameRounds_SessionId_RoundOrder' AND [object_id] = OBJECT_ID(N'[vocabulary].[FallingWordGameRounds]')) EXEC(N'CREATE UNIQUE INDEX [IX_FallingWordGameRounds_SessionId_RoundOrder] ON [vocabulary].[FallingWordGameRounds] ([SessionId], [RoundOrder]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_FallingWordGameRounds_Status_ExpiresAtUtc' AND [object_id] = OBJECT_ID(N'[vocabulary].[FallingWordGameRounds]')) EXEC(N'CREATE INDEX [IX_FallingWordGameRounds_Status_ExpiresAtUtc] ON [vocabulary].[FallingWordGameRounds] ([Status], [ExpiresAtUtc]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_FallingWordGameSessions_UserId_CreatedAtUtc' AND [object_id] = OBJECT_ID(N'[vocabulary].[FallingWordGameSessions]')) EXEC(N'CREATE INDEX [IX_FallingWordGameSessions_UserId_CreatedAtUtc] ON [vocabulary].[FallingWordGameSessions] ([UserId], [CreatedAtUtc]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_FallingWordGameSessions_UserId_Status' AND [object_id] = OBJECT_ID(N'[vocabulary].[FallingWordGameSessions]')) EXEC(N'CREATE INDEX [IX_FallingWordGameSessions_UserId_Status] ON [vocabulary].[FallingWordGameSessions] ([UserId], [Status]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_UserLearningSettings_UserId' AND [object_id] = OBJECT_ID(N'[vocabulary].[UserLearningSettings]')) EXEC(N'CREATE UNIQUE INDEX [IX_UserLearningSettings_UserId] ON [vocabulary].[UserLearningSettings] ([UserId]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_UserVocabularyProgresses_UserId_NextReviewAt' AND [object_id] = OBJECT_ID(N'[vocabulary].[UserVocabularyProgresses]')) EXEC(N'CREATE INDEX [IX_UserVocabularyProgresses_UserId_NextReviewAt] ON [vocabulary].[UserVocabularyProgresses] ([UserId], [NextReviewAt]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_UserVocabularyProgresses_UserId_SenseId' AND [object_id] = OBJECT_ID(N'[vocabulary].[UserVocabularyProgresses]')) EXEC(N'CREATE UNIQUE INDEX [IX_UserVocabularyProgresses_UserId_SenseId] ON [vocabulary].[UserVocabularyProgresses] ([UserId], [SenseId]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Vocabularies_NormalizedWord_LanguageCode' AND [object_id] = OBJECT_ID(N'[vocabulary].[Vocabularies]')) EXEC(N'CREATE INDEX [IX_Vocabularies_NormalizedWord_LanguageCode] ON [vocabulary].[Vocabularies] ([NormalizedWord], [LanguageCode]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_VocabularyCollocations_SenseId' AND [object_id] = OBJECT_ID(N'[vocabulary].[VocabularyCollocations]')) EXEC(N'CREATE INDEX [IX_VocabularyCollocations_SenseId] ON [vocabulary].[VocabularyCollocations] ([SenseId]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_VocabularyExamples_SenseId' AND [object_id] = OBJECT_ID(N'[vocabulary].[VocabularyExamples]')) EXEC(N'CREATE INDEX [IX_VocabularyExamples_SenseId] ON [vocabulary].[VocabularyExamples] ([SenseId]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_VocabularyGrammarPatterns_SenseId' AND [object_id] = OBJECT_ID(N'[vocabulary].[VocabularyGrammarPatterns]')) EXEC(N'CREATE INDEX [IX_VocabularyGrammarPatterns_SenseId] ON [vocabulary].[VocabularyGrammarPatterns] ([SenseId]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_VocabularyPronunciations_VocabularyId' AND [object_id] = OBJECT_ID(N'[vocabulary].[VocabularyPronunciations]')) EXEC(N'CREATE INDEX [IX_VocabularyPronunciations_VocabularyId] ON [vocabulary].[VocabularyPronunciations] ([VocabularyId]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_VocabularyRelations_SenseId' AND [object_id] = OBJECT_ID(N'[vocabulary].[VocabularyRelations]')) EXEC(N'CREATE INDEX [IX_VocabularyRelations_SenseId] ON [vocabulary].[VocabularyRelations] ([SenseId]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_VocabularyReviewAttempts_UserId_ClientRequestId' AND [object_id] = OBJECT_ID(N'[vocabulary].[VocabularyReviewAttempts]')) EXEC(N'CREATE UNIQUE INDEX [IX_VocabularyReviewAttempts_UserId_ClientRequestId] ON [vocabulary].[VocabularyReviewAttempts] ([UserId], [ClientRequestId]) WHERE [ClientRequestId] IS NOT NULL;');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_VocabularySenses_VocabularyId_DisplayOrder' AND [object_id] = OBJECT_ID(N'[vocabulary].[VocabularySenses]')) EXEC(N'CREATE INDEX [IX_VocabularySenses_VocabularyId_DisplayOrder] ON [vocabulary].[VocabularySenses] ([VocabularyId], [DisplayOrder]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_VocabularyWordFamilies_VocabularyId' AND [object_id] = OBJECT_ID(N'[vocabulary].[VocabularyWordFamilies]')) EXEC(N'CREATE INDEX [IX_VocabularyWordFamilies_VocabularyId] ON [vocabulary].[VocabularyWordFamilies] ([VocabularyId]);');");

            migrationBuilder.Sql("IF OBJECT_ID(N'[vocabulary].[FK_DailyVocabularyItems_DailyVocabularyPlans_PlanId]', N'F') IS NULL EXEC(N'ALTER TABLE [vocabulary].[DailyVocabularyItems] ADD CONSTRAINT [FK_DailyVocabularyItems_DailyVocabularyPlans_PlanId] FOREIGN KEY ([PlanId]) REFERENCES [vocabulary].[DailyVocabularyPlans] ([Id]) ON DELETE CASCADE;');");
            migrationBuilder.Sql("IF OBJECT_ID(N'[vocabulary].[FK_FallingWordGameRounds_FallingWordGameSessions_SessionId]', N'F') IS NULL EXEC(N'ALTER TABLE [vocabulary].[FallingWordGameRounds] ADD CONSTRAINT [FK_FallingWordGameRounds_FallingWordGameSessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [vocabulary].[FallingWordGameSessions] ([Id]) ON DELETE CASCADE;');");
            migrationBuilder.Sql("IF OBJECT_ID(N'[vocabulary].[FK_VocabularyPronunciations_Vocabularies_VocabularyId]', N'F') IS NULL EXEC(N'ALTER TABLE [vocabulary].[VocabularyPronunciations] ADD CONSTRAINT [FK_VocabularyPronunciations_Vocabularies_VocabularyId] FOREIGN KEY ([VocabularyId]) REFERENCES [vocabulary].[Vocabularies] ([Id]) ON DELETE CASCADE;');");
            migrationBuilder.Sql("IF OBJECT_ID(N'[vocabulary].[FK_VocabularySenses_Vocabularies_VocabularyId]', N'F') IS NULL EXEC(N'ALTER TABLE [vocabulary].[VocabularySenses] ADD CONSTRAINT [FK_VocabularySenses_Vocabularies_VocabularyId] FOREIGN KEY ([VocabularyId]) REFERENCES [vocabulary].[Vocabularies] ([Id]) ON DELETE CASCADE;');");
            migrationBuilder.Sql("IF OBJECT_ID(N'[vocabulary].[FK_VocabularyWordFamilies_Vocabularies_VocabularyId]', N'F') IS NULL EXEC(N'ALTER TABLE [vocabulary].[VocabularyWordFamilies] ADD CONSTRAINT [FK_VocabularyWordFamilies_Vocabularies_VocabularyId] FOREIGN KEY ([VocabularyId]) REFERENCES [vocabulary].[Vocabularies] ([Id]) ON DELETE CASCADE;');");
            migrationBuilder.Sql("IF OBJECT_ID(N'[vocabulary].[FK_VocabularyCollocations_VocabularySenses_SenseId]', N'F') IS NULL EXEC(N'ALTER TABLE [vocabulary].[VocabularyCollocations] ADD CONSTRAINT [FK_VocabularyCollocations_VocabularySenses_SenseId] FOREIGN KEY ([SenseId]) REFERENCES [vocabulary].[VocabularySenses] ([Id]) ON DELETE CASCADE;');");
            migrationBuilder.Sql("IF OBJECT_ID(N'[vocabulary].[FK_VocabularyExamples_VocabularySenses_SenseId]', N'F') IS NULL EXEC(N'ALTER TABLE [vocabulary].[VocabularyExamples] ADD CONSTRAINT [FK_VocabularyExamples_VocabularySenses_SenseId] FOREIGN KEY ([SenseId]) REFERENCES [vocabulary].[VocabularySenses] ([Id]) ON DELETE CASCADE;');");
            migrationBuilder.Sql("IF OBJECT_ID(N'[vocabulary].[FK_VocabularyGrammarPatterns_VocabularySenses_SenseId]', N'F') IS NULL EXEC(N'ALTER TABLE [vocabulary].[VocabularyGrammarPatterns] ADD CONSTRAINT [FK_VocabularyGrammarPatterns_VocabularySenses_SenseId] FOREIGN KEY ([SenseId]) REFERENCES [vocabulary].[VocabularySenses] ([Id]) ON DELETE CASCADE;');");
            migrationBuilder.Sql("IF OBJECT_ID(N'[vocabulary].[FK_VocabularyRelations_VocabularySenses_SenseId]', N'F') IS NULL EXEC(N'ALTER TABLE [vocabulary].[VocabularyRelations] ADD CONSTRAINT [FK_VocabularyRelations_VocabularySenses_SenseId] FOREIGN KEY ([SenseId]) REFERENCES [vocabulary].[VocabularySenses] ([Id]) ON DELETE CASCADE;');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "VocabularyRelations",
                schema: "vocabulary",
                newName: "VocabularyRelations",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "VocabularyGrammarPatterns",
                schema: "vocabulary",
                newName: "VocabularyGrammarPatterns",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "VocabularyExamples",
                schema: "vocabulary",
                newName: "VocabularyExamples",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "VocabularyCollocations",
                schema: "vocabulary",
                newName: "VocabularyCollocations",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "VocabularyWordFamilies",
                schema: "vocabulary",
                newName: "VocabularyWordFamilies",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "VocabularySenses",
                schema: "vocabulary",
                newName: "VocabularySenses",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "VocabularyPronunciations",
                schema: "vocabulary",
                newName: "VocabularyPronunciations",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "FallingWordGameRounds",
                schema: "vocabulary",
                newName: "FallingWordGameRounds",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "DailyVocabularyItems",
                schema: "vocabulary",
                newName: "DailyVocabularyItems",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "VocabularyReviewAttempts",
                schema: "vocabulary",
                newName: "VocabularyReviewAttempts",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "VocabularyGenerationAudits",
                schema: "vocabulary",
                newName: "VocabularyGenerationAudits",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "Vocabularies",
                schema: "vocabulary",
                newName: "Vocabularies",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "UserVocabularyProgresses",
                schema: "vocabulary",
                newName: "UserVocabularyProgresses",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "UserLearningSettings",
                schema: "vocabulary",
                newName: "UserLearningSettings",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "FallingWordGameSessions",
                schema: "vocabulary",
                newName: "FallingWordGameSessions",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "DailyVocabularyPlans",
                schema: "vocabulary",
                newName: "DailyVocabularyPlans",
                newSchema: "dbo");
        }
    }
}
