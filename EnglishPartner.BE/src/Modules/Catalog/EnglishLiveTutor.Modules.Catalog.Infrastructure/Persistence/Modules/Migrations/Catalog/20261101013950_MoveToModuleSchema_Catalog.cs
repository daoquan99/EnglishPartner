using EnglishLiveTutor.Infrastructure.Persistence;
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLiveTutor.Infrastructure.Persistence.Modules.Migrations.Catalog
{
    /// <inheritdoc />
    public partial class MoveToModuleSchema_Catalog : Migration
    {
        /// <inheritdoc />
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildGuardLegacySql(
                "catalog",
                [
                    "AiModelCatalog",
                    "CefrLevelPrompts",
                    "ConversationModePrompts",
                    "SpeechSpeeds",
                    "TeacherVoices",
                    "Topics"
                ]));

            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("catalog", "AiModelCatalog"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("catalog", "CefrLevelPrompts"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("catalog", "ConversationModePrompts"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("catalog", "SpeechSpeeds"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("catalog", "TeacherVoices"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("catalog", "Topics"));

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[catalog].[AiModelCatalog]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [catalog].[AiModelCatalog] (
                    [Id] uniqueidentifier NOT NULL,
                    [DisplayName] nvarchar(128) NOT NULL,
                    [ModelCode] nvarchar(128) NOT NULL,
                    [RoutedModelCode] nvarchar(128) NULL,
                    [SupportsTextGeneration] bit NOT NULL,
                    [SupportsStructuredOutput] bit NOT NULL,
                    [SupportsAudioInput] bit NOT NULL,
                    [SupportsPronunciationAssessment] bit NOT NULL,
                    [SupportsLiveAudio] bit NOT NULL,
                    [SupportsTranscription] bit NOT NULL,
                    [SupportsTranslation] bit NOT NULL,
                    [SupportsTts] bit NOT NULL,
                    [SupportsStreaming] bit NOT NULL,
                    [IsAvailable] bit NOT NULL,
                    [IsDeprecated] bit NOT NULL,
                    [IsEnabled] bit NOT NULL,
                    [CapabilityNote] nvarchar(500) NULL,
                    [DisplayOrder] int NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset NULL,
                    [DeletedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_AiModelCatalog] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[catalog].[CefrLevelPrompts]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [catalog].[CefrLevelPrompts] (
                    [Id] uniqueidentifier NOT NULL,
                    [CefrLevel] nvarchar(8) NOT NULL,
                    [DisplayName] nvarchar(128) NOT NULL,
                    [LevelGuidance] nvarchar(max) NOT NULL,
                    [DisplayOrder] int NOT NULL,
                    [IsActive] bit NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset NULL,
                    [DeletedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_CefrLevelPrompts] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[catalog].[ConversationModePrompts]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [catalog].[ConversationModePrompts] (
                    [Id] uniqueidentifier NOT NULL,
                    [ModeCode] nvarchar(64) NOT NULL,
                    [Category] nvarchar(64) NOT NULL,
                    [DisplayName] nvarchar(256) NOT NULL,
                    [DirectiveTemplate] nvarchar(max) NOT NULL,
                    [DisplayOrder] int NOT NULL,
                    [IsDefault] bit NOT NULL,
                    [IsActive] bit NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset NULL,
                    [DeletedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_ConversationModePrompts] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[catalog].[SpeechSpeeds]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [catalog].[SpeechSpeeds] (
                    [Id] uniqueidentifier NOT NULL,
                    [Code] nvarchar(32) NOT NULL,
                    [SpeedValue] float NOT NULL,
                    [Label] nvarchar(160) NOT NULL,
                    [DescriptionVi] nvarchar(400) NOT NULL,
                    [RecommendedCefrRange] nvarchar(32) NOT NULL,
                    [IsDefault] bit NOT NULL,
                    [IsActive] bit NOT NULL,
                    [DisplayOrder] int NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset NULL,
                    [DeletedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_SpeechSpeeds] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[catalog].[TeacherVoices]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [catalog].[TeacherVoices] (
                    [Id] nvarchar(64) NOT NULL,
                    [Name] nvarchar(64) NOT NULL,
                    [DisplayName] nvarchar(128) NOT NULL,
                    [Label] nvarchar(128) NOT NULL,
                    [Type] nvarchar(32) NOT NULL DEFAULT N''prebuilt'',
                    [Gender] nvarchar(16) NOT NULL,
                    [Persona] nvarchar(64) NOT NULL,
                    [Style] nvarchar(300) NOT NULL,
                    [Description] nvarchar(500) NOT NULL,
                    [IsFeatured] bit NOT NULL,
                    [IsEnabled] bit NOT NULL,
                    [DisplayOrder] int NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset NULL,
                    [DeletedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_TeacherVoices] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[catalog].[Topics]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [catalog].[Topics] (
                    [Id] uniqueidentifier NOT NULL,
                    [Code] nvarchar(80) NOT NULL,
                    [Name] nvarchar(256) NOT NULL,
                    [DisplayNameVi] nvarchar(256) NOT NULL,
                    [Category] nvarchar(64) NOT NULL DEFAULT N''General'',
                    [ModeCode] nvarchar(64) NULL,
                    [SuggestedCefrLevel] nvarchar(8) NOT NULL DEFAULT N''B1'',
                    [SupportsConversation] bit NOT NULL,
                    [SupportsVocabulary] bit NOT NULL,
                    [Description] nvarchar(600) NOT NULL,
                    [IsFeatured] bit NOT NULL,
                    [IsActive] bit NOT NULL,
                    [DisplayOrder] int NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset NULL,
                    [DeletedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_Topics] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_AiModelCatalog_ModelCode' AND [object_id] = OBJECT_ID(N'[catalog].[AiModelCatalog]')) EXEC(N'CREATE UNIQUE INDEX [IX_AiModelCatalog_ModelCode] ON [catalog].[AiModelCatalog] ([ModelCode]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_CefrLevelPrompts_CefrLevel' AND [object_id] = OBJECT_ID(N'[catalog].[CefrLevelPrompts]')) EXEC(N'CREATE UNIQUE INDEX [IX_CefrLevelPrompts_CefrLevel] ON [catalog].[CefrLevelPrompts] ([CefrLevel]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_ConversationModePrompts_ModeCode' AND [object_id] = OBJECT_ID(N'[catalog].[ConversationModePrompts]')) EXEC(N'CREATE UNIQUE INDEX [IX_ConversationModePrompts_ModeCode] ON [catalog].[ConversationModePrompts] ([ModeCode]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_SpeechSpeeds_Code' AND [object_id] = OBJECT_ID(N'[catalog].[SpeechSpeeds]')) EXEC(N'CREATE UNIQUE INDEX [IX_SpeechSpeeds_Code] ON [catalog].[SpeechSpeeds] ([Code]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_SpeechSpeeds_DisplayOrder' AND [object_id] = OBJECT_ID(N'[catalog].[SpeechSpeeds]')) EXEC(N'CREATE INDEX [IX_SpeechSpeeds_DisplayOrder] ON [catalog].[SpeechSpeeds] ([DisplayOrder]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_TeacherVoices_IsFeatured_DisplayOrder' AND [object_id] = OBJECT_ID(N'[catalog].[TeacherVoices]')) EXEC(N'CREATE INDEX [IX_TeacherVoices_IsFeatured_DisplayOrder] ON [catalog].[TeacherVoices] ([IsFeatured], [DisplayOrder]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Topics_Code' AND [object_id] = OBJECT_ID(N'[catalog].[Topics]')) EXEC(N'CREATE UNIQUE INDEX [IX_Topics_Code] ON [catalog].[Topics] ([Code]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Topics_IsActive_SupportsConversation_SupportsVocabulary_DisplayOrder' AND [object_id] = OBJECT_ID(N'[catalog].[Topics]')) EXEC(N'CREATE INDEX [IX_Topics_IsActive_SupportsConversation_SupportsVocabulary_DisplayOrder] ON [catalog].[Topics] ([IsActive], [SupportsConversation], [SupportsVocabulary], [DisplayOrder]);');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Topics",
                schema: "catalog",
                newName: "Topics",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "TeacherVoices",
                schema: "catalog",
                newName: "TeacherVoices",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "SpeechSpeeds",
                schema: "catalog",
                newName: "SpeechSpeeds",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "ConversationModePrompts",
                schema: "catalog",
                newName: "ConversationModePrompts",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "CefrLevelPrompts",
                schema: "catalog",
                newName: "CefrLevelPrompts",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "AiModelCatalog",
                schema: "catalog",
                newName: "AiModelCatalog",
                newSchema: "dbo");
        }
    }
}
