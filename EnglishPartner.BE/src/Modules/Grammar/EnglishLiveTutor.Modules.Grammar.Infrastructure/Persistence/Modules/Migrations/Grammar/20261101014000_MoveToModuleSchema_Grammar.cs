using EnglishLiveTutor.Infrastructure.Persistence;
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLiveTutor.Infrastructure.Persistence.Modules.Migrations.Grammar
{
    /// <inheritdoc />
    public partial class MoveToModuleSchema_Grammar : Migration
    {
        /// <inheritdoc />
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "grammar");

            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildGuardLegacySql(
                "grammar",
                [
                    "GrammarSteps",
                    "GrammarAttempts",
                    "GrammarExercises",
                    "UserGrammarStepProgress",
                    "GrammarAttemptAnswers"
                ]));

            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("grammar", "GrammarSteps"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("grammar", "GrammarAttempts"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("grammar", "GrammarExercises"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("grammar", "UserGrammarStepProgress"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("grammar", "GrammarAttemptAnswers"));

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[grammar].[GrammarSteps]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [grammar].[GrammarSteps] (
                    [Id] uniqueidentifier NOT NULL,
                    [Code] nvarchar(16) NOT NULL,
                    [CefrLevel] nvarchar(8) NOT NULL,
                    [Order] int NOT NULL,
                    [TitleVi] nvarchar(200) NOT NULL,
                    [TitleEn] nvarchar(200) NOT NULL,
                    [SummaryVi] nvarchar(500) NOT NULL,
                    [TheoryJson] nvarchar(max) NOT NULL,
                    [TheoryVersion] int NOT NULL,
                    [TheoryEditedByAdmin] bit NOT NULL,
                    [IsLevelReview] bit NOT NULL,
                    [IsPublished] bit NOT NULL,
                    [RowVersion] rowversion NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset NULL,
                    [DeletedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_GrammarSteps] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[grammar].[GrammarAttempts]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [grammar].[GrammarAttempts] (
                    [Id] uniqueidentifier NOT NULL,
                    [UserId] nvarchar(128) NOT NULL,
                    [Kind] int NOT NULL,
                    [StepId] uniqueidentifier NULL,
                    [CefrLevel] nvarchar(8) NOT NULL,
                    [ExerciseIdsJson] nvarchar(max) NOT NULL,
                    [StartedAtUtc] datetimeoffset NOT NULL,
                    [CompletedAtUtc] datetimeoffset NULL,
                    [Score100] decimal(5,2) NULL,
                    [RowVersion] rowversion NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset NULL,
                    [DeletedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_GrammarAttempts] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[grammar].[GrammarExercises]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [grammar].[GrammarExercises] (
                    [Id] uniqueidentifier NOT NULL,
                    [StepId] uniqueidentifier NOT NULL,
                    [Key] nvarchar(64) NULL,
                    [Type] int NOT NULL,
                    [Difficulty] int NOT NULL,
                    [Title] nvarchar(200) NOT NULL,
                    [Instructions] nvarchar(1000) NOT NULL,
                    [Focus] nvarchar(200) NULL,
                    [ContentJson] nvarchar(max) NOT NULL,
                    [GradingDefinitionJson] nvarchar(max) NULL,
                    [MaxPoints] decimal(6,2) NOT NULL,
                    [GradingMode] int NOT NULL,
                    [Source] int NOT NULL,
                    [OwnerUserId] nvarchar(128) NULL,
                    [AuthorUserId] nvarchar(128) NULL,
                    [Visibility] int NOT NULL,
                    [Moderation] int NOT NULL,
                    [RejectReason] nvarchar(500) NULL,
                    [ReviewedBy] nvarchar(128) NULL,
                    [ReviewedAtUtc] datetimeoffset NULL,
                    [IsCore] bit NOT NULL,
                    [ContentHash] nvarchar(64) NOT NULL,
                    [RowVersion] rowversion NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset NULL,
                    [DeletedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_GrammarExercises] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[grammar].[UserGrammarStepProgress]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [grammar].[UserGrammarStepProgress] (
                    [Id] uniqueidentifier NOT NULL,
                    [UserId] nvarchar(128) NOT NULL,
                    [StepId] uniqueidentifier NOT NULL,
                    [Status] int NOT NULL,
                    [BestScore100] decimal(5,2) NOT NULL,
                    [PassedBy] int NULL,
                    [UnlockedAtUtc] datetimeoffset NOT NULL,
                    [PassedAtUtc] datetimeoffset NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset NULL,
                    [DeletedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_UserGrammarStepProgress] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[grammar].[GrammarAttemptAnswers]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [grammar].[GrammarAttemptAnswers] (
                    [Id] uniqueidentifier NOT NULL,
                    [AttemptId] uniqueidentifier NOT NULL,
                    [ExerciseId] uniqueidentifier NOT NULL,
                    [AnswerPayloadJson] nvarchar(max) NOT NULL,
                    [Status] nvarchar(32) NOT NULL,
                    [Score100] decimal(5,2) NULL,
                    [PointsEarned] decimal(6,2) NULL,
                    [MaxPoints] decimal(6,2) NOT NULL,
                    [FeedbackJson] nvarchar(max) NULL,
                    [ContentHash] nvarchar(64) NOT NULL,
                    [SubmittedAtUtc] datetimeoffset NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_GrammarAttemptAnswers] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_GrammarAttemptAnswers_AttemptId_ExerciseId' AND [object_id] = OBJECT_ID(N'[grammar].[GrammarAttemptAnswers]')) EXEC(N'CREATE UNIQUE INDEX [IX_GrammarAttemptAnswers_AttemptId_ExerciseId] ON [grammar].[GrammarAttemptAnswers] ([AttemptId], [ExerciseId]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_GrammarAttempts_StepId' AND [object_id] = OBJECT_ID(N'[grammar].[GrammarAttempts]')) EXEC(N'CREATE INDEX [IX_GrammarAttempts_StepId] ON [grammar].[GrammarAttempts] ([StepId]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_GrammarAttempts_UserId_StartedAtUtc' AND [object_id] = OBJECT_ID(N'[grammar].[GrammarAttempts]')) EXEC(N'CREATE INDEX [IX_GrammarAttempts_UserId_StartedAtUtc] ON [grammar].[GrammarAttempts] ([UserId], [StartedAtUtc]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_GrammarExercises_OwnerUserId_StepId' AND [object_id] = OBJECT_ID(N'[grammar].[GrammarExercises]')) EXEC(N'CREATE INDEX [IX_GrammarExercises_OwnerUserId_StepId] ON [grammar].[GrammarExercises] ([OwnerUserId], [StepId]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_GrammarExercises_StepId_ContentHash' AND [object_id] = OBJECT_ID(N'[grammar].[GrammarExercises]')) EXEC(N'CREATE INDEX [IX_GrammarExercises_StepId_ContentHash] ON [grammar].[GrammarExercises] ([StepId], [ContentHash]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_GrammarExercises_StepId_Key' AND [object_id] = OBJECT_ID(N'[grammar].[GrammarExercises]')) EXEC(N'CREATE UNIQUE INDEX [IX_GrammarExercises_StepId_Key] ON [grammar].[GrammarExercises] ([StepId], [Key]) WHERE [Key] IS NOT NULL;');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_GrammarExercises_StepId_Source_Moderation_IsDeleted' AND [object_id] = OBJECT_ID(N'[grammar].[GrammarExercises]')) EXEC(N'CREATE INDEX [IX_GrammarExercises_StepId_Source_Moderation_IsDeleted] ON [grammar].[GrammarExercises] ([StepId], [Source], [Moderation], [IsDeleted]) INCLUDE ([IsCore], [OwnerUserId], [Difficulty], [Key], [CreatedAtUtc]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_GrammarSteps_CefrLevel_Order' AND [object_id] = OBJECT_ID(N'[grammar].[GrammarSteps]')) EXEC(N'CREATE UNIQUE INDEX [IX_GrammarSteps_CefrLevel_Order] ON [grammar].[GrammarSteps] ([CefrLevel], [Order]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_GrammarSteps_Code' AND [object_id] = OBJECT_ID(N'[grammar].[GrammarSteps]')) EXEC(N'CREATE UNIQUE INDEX [IX_GrammarSteps_Code] ON [grammar].[GrammarSteps] ([Code]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_UserGrammarStepProgress_StepId' AND [object_id] = OBJECT_ID(N'[grammar].[UserGrammarStepProgress]')) EXEC(N'CREATE INDEX [IX_UserGrammarStepProgress_StepId] ON [grammar].[UserGrammarStepProgress] ([StepId]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_UserGrammarStepProgress_UserId_StepId' AND [object_id] = OBJECT_ID(N'[grammar].[UserGrammarStepProgress]')) EXEC(N'CREATE UNIQUE INDEX [IX_UserGrammarStepProgress_UserId_StepId] ON [grammar].[UserGrammarStepProgress] ([UserId], [StepId]);');");

            migrationBuilder.Sql("IF OBJECT_ID(N'[grammar].[FK_GrammarAttempts_GrammarSteps_StepId]', N'F') IS NULL EXEC(N'ALTER TABLE [grammar].[GrammarAttempts] ADD CONSTRAINT [FK_GrammarAttempts_GrammarSteps_StepId] FOREIGN KEY ([StepId]) REFERENCES [grammar].[GrammarSteps] ([Id]) ON DELETE NO ACTION;');");
            migrationBuilder.Sql("IF OBJECT_ID(N'[grammar].[FK_GrammarExercises_GrammarSteps_StepId]', N'F') IS NULL EXEC(N'ALTER TABLE [grammar].[GrammarExercises] ADD CONSTRAINT [FK_GrammarExercises_GrammarSteps_StepId] FOREIGN KEY ([StepId]) REFERENCES [grammar].[GrammarSteps] ([Id]) ON DELETE NO ACTION;');");
            migrationBuilder.Sql("IF OBJECT_ID(N'[grammar].[FK_UserGrammarStepProgress_GrammarSteps_StepId]', N'F') IS NULL EXEC(N'ALTER TABLE [grammar].[UserGrammarStepProgress] ADD CONSTRAINT [FK_UserGrammarStepProgress_GrammarSteps_StepId] FOREIGN KEY ([StepId]) REFERENCES [grammar].[GrammarSteps] ([Id]) ON DELETE NO ACTION;');");
            migrationBuilder.Sql("IF OBJECT_ID(N'[grammar].[FK_GrammarAttemptAnswers_GrammarAttempts_AttemptId]', N'F') IS NULL EXEC(N'ALTER TABLE [grammar].[GrammarAttemptAnswers] ADD CONSTRAINT [FK_GrammarAttemptAnswers_GrammarAttempts_AttemptId] FOREIGN KEY ([AttemptId]) REFERENCES [grammar].[GrammarAttempts] ([Id]) ON DELETE CASCADE;');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "GrammarAttemptAnswers",
                schema: "grammar",
                newName: "GrammarAttemptAnswers",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "UserGrammarStepProgress",
                schema: "grammar",
                newName: "UserGrammarStepProgress",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "GrammarExercises",
                schema: "grammar",
                newName: "GrammarExercises",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "GrammarAttempts",
                schema: "grammar",
                newName: "GrammarAttempts",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "GrammarSteps",
                schema: "grammar",
                newName: "GrammarSteps",
                newSchema: "dbo");
        }
    }
}
