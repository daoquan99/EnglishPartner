using EnglishLiveTutor.Infrastructure.Persistence;
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLiveTutor.Shared.Infrastructure.Persistence.Migrations.Shared
{
    /// <inheritdoc />
    public partial class InitialCreate_Shared : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "shared");

            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildGuardLegacySql(
                "shared",
                [
                    "IdempotencyRecords"
                ]));

            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("shared", "IdempotencyRecords"));

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[shared].[IdempotencyRecords]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [shared].[IdempotencyRecords] (
                    [Id] uniqueidentifier NOT NULL,
                    [UserId] nvarchar(64) NOT NULL,
                    [OperationType] nvarchar(64) NOT NULL,
                    [ResourceScope] nvarchar(128) NOT NULL,
                    [OperationKey] nvarchar(128) NOT NULL,
                    [RequestFingerprint] nvarchar(128) NOT NULL,
                    [Status] nvarchar(32) NOT NULL,
                    [ResponseStatusCode] int NOT NULL,
                    [ResponsePayloadJson] nvarchar(max) NULL,
                    [LocationHeader] nvarchar(256) NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CompletedAtUtc] datetimeoffset NULL,
                    [ExpiresAtUtc] datetimeoffset NOT NULL,
                    [RowVersion] rowversion NOT NULL,
                    CONSTRAINT [PK_IdempotencyRecords] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_IdempotencyRecords_ExpiresAtUtc' AND [object_id] = OBJECT_ID(N'[shared].[IdempotencyRecords]')) EXEC(N'CREATE INDEX [IX_IdempotencyRecords_ExpiresAtUtc] ON [shared].[IdempotencyRecords] ([ExpiresAtUtc]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_IdempotencyRecords_Scope_Key' AND [object_id] = OBJECT_ID(N'[shared].[IdempotencyRecords]')) EXEC(N'CREATE UNIQUE INDEX [IX_IdempotencyRecords_Scope_Key] ON [shared].[IdempotencyRecords] ([UserId], [OperationType], [ResourceScope], [OperationKey]);');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "IdempotencyRecords",
                schema: "shared",
                newName: "IdempotencyRecords",
                newSchema: "dbo");
        }
    }
}
