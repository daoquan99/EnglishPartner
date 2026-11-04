using EnglishLiveTutor.Infrastructure.Persistence;
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLiveTutor.Infrastructure.Persistence.Modules.Migrations.Identity
{
    /// <inheritdoc />
    public partial class MoveToModuleSchema_Identity : Migration
    {
        /// <inheritdoc />
                protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildGuardLegacySql(
                "identity",
                [
                    "Permissions",
                    "Roles",
                    "Users",
                    "RolePermissions",
                    "RefreshTokens",
                    "UserRoles"
                ]));

            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("identity", "Permissions"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("identity", "Roles"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("identity", "Users"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("identity", "RolePermissions"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("identity", "RefreshTokens"));
            migrationBuilder.Sql(ModuleSchemaMigrationSql.BuildTransferTableSql("identity", "UserRoles"));

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[identity].[Permissions]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [identity].[Permissions] (
                    [Id] uniqueidentifier NOT NULL,
                    [Code] nvarchar(128) NOT NULL,
                    [Name] nvarchar(160) NOT NULL,
                    [Module] nvarchar(64) NOT NULL,
                    [Description] nvarchar(500) NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset NULL,
                    [DeletedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[identity].[Roles]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [identity].[Roles] (
                    [Id] uniqueidentifier NOT NULL,
                    [Code] nvarchar(64) NOT NULL,
                    [Name] nvarchar(128) NOT NULL,
                    [Description] nvarchar(500) NOT NULL,
                    [IsSystemRole] bit NOT NULL,
                    [IsActive] bit NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset NULL,
                    [DeletedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[identity].[Users]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [identity].[Users] (
                    [Id] uniqueidentifier NOT NULL,
                    [Username] nvarchar(64) NOT NULL,
                    [Email] nvarchar(256) NOT NULL,
                    [NormalizedEmail] nvarchar(256) NOT NULL,
                    [FullName] nvarchar(160) NOT NULL,
                    [PasswordHash] nvarchar(512) NOT NULL,
                    [AvatarUrl] nvarchar(512) NULL,
                    [PreferredCefrLevel] nvarchar(8) NOT NULL DEFAULT N''B1'',
                    [PreferredVoiceName] nvarchar(64) NOT NULL DEFAULT N''Aoede'',
                    [TimeZoneId] nvarchar(64) NOT NULL DEFAULT N''Asia/Ho_Chi_Minh'',
                    [IsActive] bit NOT NULL,
                    [LastLoginAtUtc] datetimeoffset NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset NULL,
                    [DeletedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[identity].[RolePermissions]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [identity].[RolePermissions] (
                    [Id] uniqueidentifier NOT NULL,
                    [RoleId] uniqueidentifier NOT NULL,
                    [PermissionId] uniqueidentifier NOT NULL,
                    [GrantedAtUtc] datetimeoffset NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[identity].[RefreshTokens]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [identity].[RefreshTokens] (
                    [Id] uniqueidentifier NOT NULL,
                    [UserId] uniqueidentifier NOT NULL,
                    [Token] nvarchar(256) NOT NULL,
                    [ExpiresAtUtc] datetimeoffset NOT NULL,
                    [RevokedAtUtc] datetimeoffset NULL,
                    [RevokedReason] nvarchar(256) NULL,
                    [ReplacedByToken] nvarchar(256) NULL,
                    [CreatedByIp] nvarchar(64) NULL,
                    [DeviceInfo] nvarchar(256) NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[identity].[UserRoles]', N'U') IS NULL
                BEGIN
                    EXEC(N'CREATE TABLE [identity].[UserRoles] (
                    [Id] uniqueidentifier NOT NULL,
                    [UserId] uniqueidentifier NOT NULL,
                    [RoleId] uniqueidentifier NOT NULL,
                    [AssignedAtUtc] datetimeoffset NOT NULL,
                    [CreatedAtUtc] datetimeoffset NOT NULL,
                    [CreatedBy] nvarchar(64) NULL,
                    [UpdatedAtUtc] datetimeoffset NULL,
                    [UpdatedBy] nvarchar(64) NULL,
                    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([Id])
                );');
                END
                """);

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Permissions_Code' AND [object_id] = OBJECT_ID(N'[identity].[Permissions]')) EXEC(N'CREATE UNIQUE INDEX [IX_Permissions_Code] ON [identity].[Permissions] ([Code]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_RefreshTokens_Token' AND [object_id] = OBJECT_ID(N'[identity].[RefreshTokens]')) EXEC(N'CREATE UNIQUE INDEX [IX_RefreshTokens_Token] ON [identity].[RefreshTokens] ([Token]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_RefreshTokens_UserId' AND [object_id] = OBJECT_ID(N'[identity].[RefreshTokens]')) EXEC(N'CREATE INDEX [IX_RefreshTokens_UserId] ON [identity].[RefreshTokens] ([UserId]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_RolePermissions_RoleId_PermissionId' AND [object_id] = OBJECT_ID(N'[identity].[RolePermissions]')) EXEC(N'CREATE UNIQUE INDEX [IX_RolePermissions_RoleId_PermissionId] ON [identity].[RolePermissions] ([RoleId], [PermissionId]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Roles_Code' AND [object_id] = OBJECT_ID(N'[identity].[Roles]')) EXEC(N'CREATE UNIQUE INDEX [IX_Roles_Code] ON [identity].[Roles] ([Code]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_UserRoles_UserId_RoleId' AND [object_id] = OBJECT_ID(N'[identity].[UserRoles]')) EXEC(N'CREATE UNIQUE INDEX [IX_UserRoles_UserId_RoleId] ON [identity].[UserRoles] ([UserId], [RoleId]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Users_NormalizedEmail' AND [object_id] = OBJECT_ID(N'[identity].[Users]')) EXEC(N'CREATE UNIQUE INDEX [IX_Users_NormalizedEmail] ON [identity].[Users] ([NormalizedEmail]);');");
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [name] = N'IX_Users_Username' AND [object_id] = OBJECT_ID(N'[identity].[Users]')) EXEC(N'CREATE UNIQUE INDEX [IX_Users_Username] ON [identity].[Users] ([Username]);');");

            migrationBuilder.Sql("IF OBJECT_ID(N'[identity].[FK_RolePermissions_Roles_RoleId]', N'F') IS NULL EXEC(N'ALTER TABLE [identity].[RolePermissions] ADD CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [identity].[Roles] ([Id]) ON DELETE CASCADE;');");
            migrationBuilder.Sql("IF OBJECT_ID(N'[identity].[FK_RefreshTokens_Users_UserId]', N'F') IS NULL EXEC(N'ALTER TABLE [identity].[RefreshTokens] ADD CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE CASCADE;');");
            migrationBuilder.Sql("IF OBJECT_ID(N'[identity].[FK_UserRoles_Users_UserId]', N'F') IS NULL EXEC(N'ALTER TABLE [identity].[UserRoles] ADD CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE CASCADE;');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "UserRoles",
                schema: "identity",
                newName: "UserRoles",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "RefreshTokens",
                schema: "identity",
                newName: "RefreshTokens",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "RolePermissions",
                schema: "identity",
                newName: "RolePermissions",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "Users",
                schema: "identity",
                newName: "Users",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "Roles",
                schema: "identity",
                newName: "Roles",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "Permissions",
                schema: "identity",
                newName: "Permissions",
                newSchema: "dbo");
        }
    }
}
