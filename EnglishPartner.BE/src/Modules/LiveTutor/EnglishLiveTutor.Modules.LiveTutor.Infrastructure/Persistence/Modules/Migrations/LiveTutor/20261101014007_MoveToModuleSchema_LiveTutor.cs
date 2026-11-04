using EnglishLiveTutor.Infrastructure.Persistence;
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLiveTutor.Infrastructure.Persistence.Modules.Migrations.LiveTutor
{
    /// <inheritdoc />
    public partial class MoveToModuleSchema_LiveTutor : Migration
    {
        /// <inheritdoc />
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "livetutor");

            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildGuardLegacySql(
                "livetutor",
                [
                    "Conversations",
                    "Corrections",
                    "Messages"
                ]));

            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("livetutor", "Conversations"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("livetutor", "Corrections"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("livetutor", "Messages"));

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[livetutor].[Conversations]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [livetutor].[Conversations] (
                    [Id] nvarchar(64) NOT NULL,
                    [UserId] nvarchar(64) NOT NULL,
                    [Title] nvarchar(256) NOT NULL,
                    [CefrLevel] nvarchar(16) NOT NULL,
                    [ExtendedThinking] bit NOT NULL,
                    [VoiceName] nvarchar(64) NOT NULL,
                    [SpeechSpeed] float NOT NULL,
                    [ConversationMode] nvarchar(64) NOT NULL DEFAULT N''daily_conversation'',
                    [Topic] nvarchar(512) NOT NULL DEFAULT N'''',
                    [CustomSystemPrompt] nvarchar(max) NULL,
                    [CreatedAt] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAt] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset NULL,
                    [DeletedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_Conversations] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[livetutor].[Corrections]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [livetutor].[Corrections] (
                    [Id] nvarchar(64) NOT NULL,
                    [ConversationId] nvarchar(64) NOT NULL,
                    [TurnId] nvarchar(64) NOT NULL,
                    [OriginalText] nvarchar(max) NOT NULL,
                    [CorrectedText] nvarchar(max) NOT NULL,
                    [ErrorCategory] nvarchar(64) NOT NULL,
                    [ExplanationVi] nvarchar(max) NOT NULL,
                    [CefrTip] nvarchar(max) NOT NULL,
                    [CreatedAt] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_Corrections] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[livetutor].[Messages]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [livetutor].[Messages] (
                    [Id] nvarchar(128) NOT NULL,
                    [ConversationId] nvarchar(64) NOT NULL,
                    [TurnId] nvarchar(64) NOT NULL,
                    [Speaker] nvarchar(16) NOT NULL,
                    [TranscriptLine1] nvarchar(max) NOT NULL,
                    [TranslationLine2] nvarchar(max) NOT NULL,
                    [IsFinal] bit NOT NULL,
                    [SequenceOrder] int NOT NULL,
                    [CreatedAt] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_Messages] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Conversations_UserId_UpdatedAt' AND [object_id] = OBJECT_ID(N'[livetutor].[Conversations]')) EXEC(N'CREATE INDEX [IX_Conversations_UserId_UpdatedAt] ON [livetutor].[Conversations] ([UserId], [UpdatedAt]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Corrections_ConversationId' AND [object_id] = OBJECT_ID(N'[livetutor].[Corrections]')) EXEC(N'CREATE INDEX [IX_Corrections_ConversationId] ON [livetutor].[Corrections] ([ConversationId]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Messages_ConversationId_SequenceOrder' AND [object_id] = OBJECT_ID(N'[livetutor].[Messages]')) EXEC(N'CREATE INDEX [IX_Messages_ConversationId_SequenceOrder] ON [livetutor].[Messages] ([ConversationId], [SequenceOrder]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Messages_ConversationId_TurnId_Speaker' AND [object_id] = OBJECT_ID(N'[livetutor].[Messages]')) EXEC(N'CREATE UNIQUE INDEX [IX_Messages_ConversationId_TurnId_Speaker] ON [livetutor].[Messages] ([ConversationId], [TurnId], [Speaker]);');");

            migrationBuilder.Sql("IF OBJECT_ID(N'[livetutor].[FK_Corrections_Conversations_ConversationId]', N'F') IS NULL EXEC(N'ALTER TABLE [livetutor].[Corrections] ADD CONSTRAINT [FK_Corrections_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [livetutor].[Conversations] ([Id]) ON DELETE CASCADE;');");
            migrationBuilder.Sql("IF OBJECT_ID(N'[livetutor].[FK_Messages_Conversations_ConversationId]', N'F') IS NULL EXEC(N'ALTER TABLE [livetutor].[Messages] ADD CONSTRAINT [FK_Messages_Conversations_ConversationId] FOREIGN KEY ([ConversationId]) REFERENCES [livetutor].[Conversations] ([Id]) ON DELETE CASCADE;');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Messages",
                schema: "livetutor",
                newName: "Messages",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "Corrections",
                schema: "livetutor",
                newName: "Corrections",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "Conversations",
                schema: "livetutor",
                newName: "Conversations",
                newSchema: "dbo");
        }
    }
}
