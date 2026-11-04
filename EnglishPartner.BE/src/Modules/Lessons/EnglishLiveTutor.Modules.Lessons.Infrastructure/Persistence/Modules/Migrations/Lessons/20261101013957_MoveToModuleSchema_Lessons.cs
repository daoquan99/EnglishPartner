using EnglishLiveTutor.Infrastructure.Persistence;
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLiveTutor.Infrastructure.Persistence.Modules.Migrations.Lessons
{
    /// <inheritdoc />
    public partial class MoveToModuleSchema_Lessons : Migration
    {
        /// <inheritdoc />
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "lessons");

            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildGuardLegacySql(
                "lessons",
                [
                    "LessonAttempts",
                    "LessonRolePlaySessions",
                    "Lessons",
                    "ExerciseAnswers",
                    "LessonRolePlayTurns",
                    "Exercises"
                ]));

            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("lessons", "LessonAttempts"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("lessons", "LessonRolePlaySessions"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("lessons", "Lessons"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("lessons", "ExerciseAnswers"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("lessons", "LessonRolePlayTurns"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("lessons", "Exercises"));

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[lessons].[LessonAttempts]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [lessons].[LessonAttempts] (
                    [Id] uniqueidentifier NOT NULL,
                    [LessonId] uniqueidentifier NOT NULL,
                    [UserId] nvarchar(64) NOT NULL,
                    [PinnedLessonVersion] int NOT NULL,
                    [AttemptRevision] int NOT NULL,
                    [Status] nvarchar(32) NOT NULL,
                    [StartedAtUtc] datetimeoffset NOT NULL,
                    [CompletedAtUtc] datetimeoffset NULL,
                    [CurrentSubtotalPoints] decimal(6,2) NOT NULL,
                    [TotalMaxPoints] decimal(6,2) NOT NULL,
                    [FinalScore100] decimal(5,2) NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset NULL,
                    [DeletedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_LessonAttempts] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[lessons].[LessonRolePlaySessions]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [lessons].[LessonRolePlaySessions] (
                    [Id] uniqueidentifier NOT NULL,
                    [UserId] nvarchar(64) NOT NULL,
                    [Scenario] nvarchar(1000) NOT NULL,
                    [AiRole] nvarchar(100) NOT NULL,
                    [InitialGreeting] nvarchar(1000) NOT NULL,
                    [MaxTurns] int NOT NULL,
                    [CurrentTurnIndex] int NOT NULL,
                    [Status] nvarchar(32) NOT NULL,
                    [EndedAtUtc] datetimeoffset NULL,
                    [ClaimedUntilUtc] datetimeoffset NULL,
                    [Version] int NOT NULL,
                    [RowVersion] rowversion NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset NULL,
                    [DeletedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_LessonRolePlaySessions] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[lessons].[Lessons]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [lessons].[Lessons] (
                    [Id] uniqueidentifier NOT NULL,
                    [Title] nvarchar(256) NOT NULL,
                    [CefrLevel] nvarchar(8) NOT NULL,
                    [Topic] nvarchar(128) NOT NULL,
                    [LearningObjectivesJson] nvarchar(max) NOT NULL,
                    [TargetVocabularyJson] nvarchar(max) NOT NULL,
                    [TargetGrammarJson] nvarchar(max) NOT NULL,
                    [Content] nvarchar(max) NOT NULL,
                    [EstimatedMinutes] int NOT NULL,
                    [Status] nvarchar(32) NOT NULL,
                    [Version] int NOT NULL,
                    [ParentLessonId] uniqueidentifier NULL,
                    [RowVersion] rowversion NULL,
                    [PublishedAtUtc] datetimeoffset NULL,
                    [ArchivedAtUtc] datetimeoffset NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset NULL,
                    [DeletedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_Lessons] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[lessons].[ExerciseAnswers]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [lessons].[ExerciseAnswers] (
                    [Id] uniqueidentifier NOT NULL,
                    [AttemptId] uniqueidentifier NOT NULL,
                    [ExerciseId] uniqueidentifier NOT NULL,
                    [SubQuestionId] uniqueidentifier NULL,
                    [GradingMode] nvarchar(32) NOT NULL,
                    [MaxPoints] decimal(6,2) NOT NULL,
                    [Status] nvarchar(32) NOT NULL,
                    [Score100] decimal(5,2) NULL,
                    [PointsEarned] decimal(6,2) NULL,
                    [AnswerPayloadJson] nvarchar(max) NOT NULL,
                    [GradingResultJson] nvarchar(max) NULL,
                    [LeaseExpiresAtUtc] datetimeoffset NULL,
                    [SubmittedAtUtc] datetimeoffset NOT NULL,
                    [GradedAtUtc] datetimeoffset NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_ExerciseAnswers] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[lessons].[LessonRolePlayTurns]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [lessons].[LessonRolePlayTurns] (
                    [Id] uniqueidentifier NOT NULL,
                    [SessionId] uniqueidentifier NOT NULL,
                    [SequenceIndex] int NOT NULL,
                    [TurnIndex] int NOT NULL,
                    [Role] nvarchar(32) NOT NULL,
                    [Message] nvarchar(max) NOT NULL,
                    [AudioAssetId] uniqueidentifier NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_LessonRolePlayTurns] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[lessons].[Exercises]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [lessons].[Exercises] (
                    [Id] uniqueidentifier NOT NULL,
                    [LessonId] uniqueidentifier NOT NULL,
                    [Key] nvarchar(64) NOT NULL,
                    [Type] int NOT NULL,
                    [Title] nvarchar(256) NOT NULL,
                    [Instructions] nvarchar(2000) NOT NULL,
                    [Order] int NOT NULL,
                    [MaxPoints] decimal(6,2) NOT NULL,
                    [GradingMode] nvarchar(32) NOT NULL,
                    [SkillTagsJson] nvarchar(max) NULL,
                    [ContentJson] nvarchar(max) NOT NULL,
                    [GradingDefinitionJson] nvarchar(max) NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_Exercises] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_ExerciseAnswers_AttemptId_ExerciseId_SubQuestionId' AND [object_id] = OBJECT_ID(N'[lessons].[ExerciseAnswers]')) EXEC(N'CREATE UNIQUE INDEX [IX_ExerciseAnswers_AttemptId_ExerciseId_SubQuestionId] ON [lessons].[ExerciseAnswers] ([AttemptId], [ExerciseId], [SubQuestionId]) WHERE [SubQuestionId] IS NOT NULL;');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_ExerciseAnswers_Status' AND [object_id] = OBJECT_ID(N'[lessons].[ExerciseAnswers]')) EXEC(N'CREATE INDEX [IX_ExerciseAnswers_Status] ON [lessons].[ExerciseAnswers] ([Status]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Exercises_LessonId_Key' AND [object_id] = OBJECT_ID(N'[lessons].[Exercises]')) EXEC(N'CREATE UNIQUE INDEX [IX_Exercises_LessonId_Key] ON [lessons].[Exercises] ([LessonId], [Key]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Exercises_LessonId_Order' AND [object_id] = OBJECT_ID(N'[lessons].[Exercises]')) EXEC(N'CREATE INDEX [IX_Exercises_LessonId_Order] ON [lessons].[Exercises] ([LessonId], [Order]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_LessonAttempts_Status' AND [object_id] = OBJECT_ID(N'[lessons].[LessonAttempts]')) EXEC(N'CREATE INDEX [IX_LessonAttempts_Status] ON [lessons].[LessonAttempts] ([Status]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_LessonAttempts_UserId_LessonId' AND [object_id] = OBJECT_ID(N'[lessons].[LessonAttempts]')) EXEC(N'CREATE INDEX [IX_LessonAttempts_UserId_LessonId] ON [lessons].[LessonAttempts] ([UserId], [LessonId]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_LessonRolePlaySessions_CreatedAtUtc' AND [object_id] = OBJECT_ID(N'[lessons].[LessonRolePlaySessions]')) EXEC(N'CREATE INDEX [IX_LessonRolePlaySessions_CreatedAtUtc] ON [lessons].[LessonRolePlaySessions] ([CreatedAtUtc]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_LessonRolePlaySessions_UserId_Status' AND [object_id] = OBJECT_ID(N'[lessons].[LessonRolePlaySessions]')) EXEC(N'CREATE INDEX [IX_LessonRolePlaySessions_UserId_Status] ON [lessons].[LessonRolePlaySessions] ([UserId], [Status]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_LessonRolePlayTurns_SessionId_SequenceIndex' AND [object_id] = OBJECT_ID(N'[lessons].[LessonRolePlayTurns]')) EXEC(N'CREATE UNIQUE INDEX [IX_LessonRolePlayTurns_SessionId_SequenceIndex] ON [lessons].[LessonRolePlayTurns] ([SessionId], [SequenceIndex]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_LessonRolePlayTurns_SessionId_TurnIndex_Role' AND [object_id] = OBJECT_ID(N'[lessons].[LessonRolePlayTurns]')) EXEC(N'CREATE UNIQUE INDEX [IX_LessonRolePlayTurns_SessionId_TurnIndex_Role] ON [lessons].[LessonRolePlayTurns] ([SessionId], [TurnIndex], [Role]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Lessons_CefrLevel' AND [object_id] = OBJECT_ID(N'[lessons].[Lessons]')) EXEC(N'CREATE INDEX [IX_Lessons_CefrLevel] ON [lessons].[Lessons] ([CefrLevel]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Lessons_Id_Version' AND [object_id] = OBJECT_ID(N'[lessons].[Lessons]')) EXEC(N'CREATE INDEX [IX_Lessons_Id_Version] ON [lessons].[Lessons] ([Id], [Version]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Lessons_Status' AND [object_id] = OBJECT_ID(N'[lessons].[Lessons]')) EXEC(N'CREATE INDEX [IX_Lessons_Status] ON [lessons].[Lessons] ([Status]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Lessons_Topic' AND [object_id] = OBJECT_ID(N'[lessons].[Lessons]')) EXEC(N'CREATE INDEX [IX_Lessons_Topic] ON [lessons].[Lessons] ([Topic]);');");

            migrationBuilder.Sql("IF OBJECT_ID(N'[lessons].[FK_ExerciseAnswers_LessonAttempts_AttemptId]', N'F') IS NULL EXEC(N'ALTER TABLE [lessons].[ExerciseAnswers] ADD CONSTRAINT [FK_ExerciseAnswers_LessonAttempts_AttemptId] FOREIGN KEY ([AttemptId]) REFERENCES [lessons].[LessonAttempts] ([Id]) ON DELETE CASCADE;');");
            migrationBuilder.Sql("IF OBJECT_ID(N'[lessons].[FK_LessonRolePlayTurns_LessonRolePlaySessions_SessionId]', N'F') IS NULL EXEC(N'ALTER TABLE [lessons].[LessonRolePlayTurns] ADD CONSTRAINT [FK_LessonRolePlayTurns_LessonRolePlaySessions_SessionId] FOREIGN KEY ([SessionId]) REFERENCES [lessons].[LessonRolePlaySessions] ([Id]) ON DELETE CASCADE;');");
            migrationBuilder.Sql("IF OBJECT_ID(N'[lessons].[FK_Exercises_Lessons_LessonId]', N'F') IS NULL EXEC(N'ALTER TABLE [lessons].[Exercises] ADD CONSTRAINT [FK_Exercises_Lessons_LessonId] FOREIGN KEY ([LessonId]) REFERENCES [lessons].[Lessons] ([Id]) ON DELETE CASCADE;');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Exercises",
                schema: "lessons",
                newName: "Exercises",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LessonRolePlayTurns",
                schema: "lessons",
                newName: "LessonRolePlayTurns",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ExerciseAnswers",
                schema: "lessons",
                newName: "ExerciseAnswers",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "Lessons",
                schema: "lessons",
                newName: "Lessons",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LessonRolePlaySessions",
                schema: "lessons",
                newName: "LessonRolePlaySessions",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "LessonAttempts",
                schema: "lessons",
                newName: "LessonAttempts",
                newSchema: "dbo");
        }
    }
}
