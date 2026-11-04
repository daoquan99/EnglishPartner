using EnglishLiveTutor.Infrastructure.Persistence;
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLiveTutor.Infrastructure.Persistence.Modules.Migrations.Speech
{
    /// <inheritdoc />
    public partial class MoveToModuleSchema_Speech : Migration
    {
        /// <inheritdoc />
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "speech");

            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildGuardLegacySql(
                "speech",
                [
                    "AudioRecordings",
                    "GeneratedPronunciationAudios",
                    "PronunciationAssessments",
                    "ScoringRubrics",
                    "PronunciationCriterionScores",
                    "ScoringRubricCriteria"
                ]));

            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("speech", "AudioRecordings"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("speech", "GeneratedPronunciationAudios"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("speech", "PronunciationAssessments"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("speech", "ScoringRubrics"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("speech", "PronunciationCriterionScores"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("speech", "ScoringRubricCriteria"));

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[speech].[AudioRecordings]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [speech].[AudioRecordings] (
                    [Id] uniqueidentifier NOT NULL,
                    [UserId] nvarchar(64) NOT NULL,
                    [StorageKey] nvarchar(256) NOT NULL,
                    [ContentType] nvarchar(64) NOT NULL DEFAULT N''audio/webm'',
                    [FileSizeBytes] bigint NOT NULL,
                    [DurationMilliseconds] int NOT NULL,
                    [ExpiresAt] datetimeoffset NULL,
                    [DeletionStatus] nvarchar(32) NOT NULL DEFAULT N''Retained'',
                    [CreatedAt] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset NULL,
                    [DeletedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_AudioRecordings] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[speech].[GeneratedPronunciationAudios]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [speech].[GeneratedPronunciationAudios] (
                    [Id] uniqueidentifier NOT NULL,
                    [TargetType] nvarchar(32) NOT NULL,
                    [TargetId] uniqueidentifier NOT NULL,
                    [VoiceName] nvarchar(64) NOT NULL,
                    [SpeechSpeed] decimal(3,2) NOT NULL,
                    [Accent] nvarchar(16) NOT NULL DEFAULT N''US'',
                    [ModelCode] nvarchar(128) NOT NULL,
                    [TextSnapshot] nvarchar(2000) NOT NULL,
                    [TextHash] nvarchar(64) NOT NULL,
                    [AudioData] varbinary(max) NOT NULL,
                    [MimeType] nvarchar(64) NOT NULL DEFAULT N''audio/wav'',
                    [SampleRate] int NOT NULL DEFAULT 24000,
                    [DurationMs] int NOT NULL,
                    [SystemPromptUsed] nvarchar(4000) NOT NULL,
                    [IsValid] bit NOT NULL DEFAULT CAST(1 AS bit),
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset NULL,
                    [DeletedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_GeneratedPronunciationAudios] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[speech].[PronunciationAssessments]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [speech].[PronunciationAssessments] (
                    [Id] uniqueidentifier NOT NULL,
                    [UserId] nvarchar(64) NOT NULL,
                    [VocabularyId] uniqueidentifier NOT NULL,
                    [SenseId] uniqueidentifier NOT NULL,
                    [TargetType] nvarchar(32) NOT NULL DEFAULT N''Word'',
                    [TargetId] uniqueidentifier NULL,
                    [ReferenceText] nvarchar(1000) NOT NULL,
                    [Accent] nvarchar(16) NOT NULL DEFAULT N''US'',
                    [ModelCode] nvarchar(128) NOT NULL,
                    [RubricId] uniqueidentifier NOT NULL,
                    [RubricVersion] int NOT NULL,
                    [AudioRecordingId] uniqueidentifier NOT NULL,
                    [RecognizedText] nvarchar(1000) NULL,
                    [OverallScore] decimal(5,2) NULL,
                    [Confidence] decimal(5,2) NULL,
                    [Status] nvarchar(32) NOT NULL DEFAULT N''Completed'',
                    [Feedback] nvarchar(max) NULL,
                    [StrengthsJson] nvarchar(max) NULL,
                    [ImprovementsJson] nvarchar(max) NULL,
                    [SuggestedPracticeJson] nvarchar(max) NULL,
                    [ErrorCode] nvarchar(64) NULL,
                    [CompletedAt] datetimeoffset NULL,
                    [CreatedAt] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset NULL,
                    [DeletedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_PronunciationAssessments] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[speech].[ScoringRubrics]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [speech].[ScoringRubrics] (
                    [Id] uniqueidentifier NOT NULL,
                    [Code] nvarchar(64) NOT NULL,
                    [Name] nvarchar(160) NOT NULL,
                    [Description] nvarchar(500) NOT NULL,
                    [Version] int NOT NULL,
                    [AssessmentType] nvarchar(32) NOT NULL DEFAULT N''Pronunciation'',
                    [AssessmentScope] nvarchar(32) NOT NULL DEFAULT N''Pronunciation'',
                    [ExerciseType] int NULL,
                    [SubQuestionKind] nvarchar(32) NULL,
                    [CefrLevel] nvarchar(8) NOT NULL DEFAULT N''All'',
                    [ApplicableExerciseTypes] nvarchar(256) NULL,
                    [IsActive] bit NOT NULL,
                    [CreatedAt] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAt] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset NULL,
                    [DeletedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_ScoringRubrics] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[speech].[PronunciationCriterionScores]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [speech].[PronunciationCriterionScores] (
                    [Id] uniqueidentifier NOT NULL,
                    [AssessmentId] uniqueidentifier NOT NULL,
                    [CriterionCode] nvarchar(64) NOT NULL,
                    [CriterionName] nvarchar(128) NOT NULL,
                    [CriterionDescriptionSnapshot] nvarchar(500) NOT NULL,
                    [WeightSnapshot] decimal(5,2) NOT NULL,
                    [RawScore] decimal(5,2) NOT NULL,
                    [WeightedScore] decimal(5,2) NOT NULL,
                    [Feedback] nvarchar(500) NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_PronunciationCriterionScores] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[speech].[ScoringRubricCriteria]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [speech].[ScoringRubricCriteria] (
                    [Id] uniqueidentifier NOT NULL,
                    [RubricId] uniqueidentifier NOT NULL,
                    [Code] nvarchar(64) NOT NULL,
                    [Name] nvarchar(128) NOT NULL,
                    [Description] nvarchar(500) NOT NULL,
                    [Weight] decimal(5,2) NOT NULL,
                    [MinimumScore] decimal(5,2) NOT NULL DEFAULT 0.0,
                    [MaximumScore] decimal(5,2) NOT NULL DEFAULT 100.0,
                    [DisplayOrder] int NOT NULL,
                    [IsEnabled] bit NOT NULL,
                    [DescriptorLevel0] nvarchar(1000) NOT NULL,
                    [DescriptorLevel1] nvarchar(1000) NOT NULL,
                    [DescriptorLevel2] nvarchar(1000) NOT NULL,
                    [DescriptorLevel3] nvarchar(1000) NOT NULL,
                    [DescriptorLevel4] nvarchar(1000) NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_ScoringRubricCriteria] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_GeneratedPronunciationAudios_Lookup' AND [object_id] = OBJECT_ID(N'[speech].[GeneratedPronunciationAudios]')) EXEC(N'CREATE INDEX [IX_GeneratedPronunciationAudios_Lookup] ON [speech].[GeneratedPronunciationAudios] ([TargetType], [TargetId], [VoiceName], [SpeechSpeed], [Accent], [ModelCode], [TextHash]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_GeneratedPronunciationAudios_TargetType_VoiceName_SpeechSpeed_TargetId' AND [object_id] = OBJECT_ID(N'[speech].[GeneratedPronunciationAudios]')) EXEC(N'CREATE INDEX [IX_GeneratedPronunciationAudios_TargetType_VoiceName_SpeechSpeed_TargetId] ON [speech].[GeneratedPronunciationAudios] ([TargetType], [VoiceName], [SpeechSpeed], [TargetId]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_PronunciationAssessments_UserId_VocabularyId_CreatedAt' AND [object_id] = OBJECT_ID(N'[speech].[PronunciationAssessments]')) EXEC(N'CREATE INDEX [IX_PronunciationAssessments_UserId_VocabularyId_CreatedAt] ON [speech].[PronunciationAssessments] ([UserId], [VocabularyId], [CreatedAt]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_PronunciationCriterionScores_AssessmentId' AND [object_id] = OBJECT_ID(N'[speech].[PronunciationCriterionScores]')) EXEC(N'CREATE INDEX [IX_PronunciationCriterionScores_AssessmentId] ON [speech].[PronunciationCriterionScores] ([AssessmentId]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_ScoringRubricCriteria_RubricId' AND [object_id] = OBJECT_ID(N'[speech].[ScoringRubricCriteria]')) EXEC(N'CREATE INDEX [IX_ScoringRubricCriteria_RubricId] ON [speech].[ScoringRubricCriteria] ([RubricId]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_ScoringRubrics_Code_Version' AND [object_id] = OBJECT_ID(N'[speech].[ScoringRubrics]')) EXEC(N'CREATE UNIQUE INDEX [IX_ScoringRubrics_Code_Version] ON [speech].[ScoringRubrics] ([Code], [Version]);');");

            migrationBuilder.Sql("IF OBJECT_ID(N'[speech].[FK_PronunciationCriterionScores_PronunciationAssessments_AssessmentId]', N'F') IS NULL EXEC(N'ALTER TABLE [speech].[PronunciationCriterionScores] ADD CONSTRAINT [FK_PronunciationCriterionScores_PronunciationAssessments_AssessmentId] FOREIGN KEY ([AssessmentId]) REFERENCES [speech].[PronunciationAssessments] ([Id]) ON DELETE CASCADE;');");
            migrationBuilder.Sql("IF OBJECT_ID(N'[speech].[FK_ScoringRubricCriteria_ScoringRubrics_RubricId]', N'F') IS NULL EXEC(N'ALTER TABLE [speech].[ScoringRubricCriteria] ADD CONSTRAINT [FK_ScoringRubricCriteria_ScoringRubrics_RubricId] FOREIGN KEY ([RubricId]) REFERENCES [speech].[ScoringRubrics] ([Id]) ON DELETE CASCADE;');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "ScoringRubricCriteria",
                schema: "speech",
                newName: "ScoringRubricCriteria",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "PronunciationCriterionScores",
                schema: "speech",
                newName: "PronunciationCriterionScores",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ScoringRubrics",
                schema: "speech",
                newName: "ScoringRubrics",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "PronunciationAssessments",
                schema: "speech",
                newName: "PronunciationAssessments",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "GeneratedPronunciationAudios",
                schema: "speech",
                newName: "GeneratedPronunciationAudios",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "AudioRecordings",
                schema: "speech",
                newName: "AudioRecordings",
                newSchema: "dbo");
        }
    }
}
