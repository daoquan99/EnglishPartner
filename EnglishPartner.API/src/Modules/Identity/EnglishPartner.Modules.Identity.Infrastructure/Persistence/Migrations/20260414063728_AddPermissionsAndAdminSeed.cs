using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EnglishPartner.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPermissionsAndAdminSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "identity",
                table: "permissions",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "Description", "Module", "Name", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0001-000000000001"), "curriculum.topics.write", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "curriculum", "Write Topics", null, null },
                    { new Guid("00000000-0000-0000-0001-000000000002"), "curriculum.vocabulary.write", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "curriculum", "Write Vocabulary", null, null },
                    { new Guid("00000000-0000-0000-0001-000000000003"), "curriculum.grammar.write", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "curriculum", "Write Grammar", null, null },
                    { new Guid("00000000-0000-0000-0001-000000000004"), "curriculum.exercise.write", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "curriculum", "Write Exercise", null, null },
                    { new Guid("00000000-0000-0000-0001-000000000005"), "curriculum.conversation.write", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "curriculum", "Write Conversation", null, null },
                    { new Guid("00000000-0000-0000-0001-000000000006"), "curriculum.sentence.write", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "curriculum", "Write Sentence", null, null },
                    { new Guid("00000000-0000-0000-0001-000000000007"), "curriculum.generate", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "curriculum", "curriculum.generate", null, null },
                    { new Guid("00000000-0000-0000-0001-000000000008"), "learning.admin.view", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "learning", "View Admin", null, null },
                    { new Guid("00000000-0000-0000-0001-000000000009"), "identity.users.manage", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "identity", "Manage Users", null, null },
                    { new Guid("00000000-0000-0000-0001-000000000010"), "identity.roles.manage", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "identity", "Manage Roles", null, null },
                    { new Guid("00000000-0000-0000-0001-000000000011"), "ai.logs.read", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "ai", "Read Logs", null, null }
                });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "role_permissions",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0003-000000000001"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("00000000-0000-0000-0001-000000000001"), new Guid("00000000-0000-0000-0000-000000000002"), null, null },
                    { new Guid("00000000-0000-0000-0003-000000000002"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("00000000-0000-0000-0001-000000000002"), new Guid("00000000-0000-0000-0000-000000000002"), null, null },
                    { new Guid("00000000-0000-0000-0003-000000000003"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("00000000-0000-0000-0001-000000000003"), new Guid("00000000-0000-0000-0000-000000000002"), null, null },
                    { new Guid("00000000-0000-0000-0003-000000000004"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("00000000-0000-0000-0001-000000000004"), new Guid("00000000-0000-0000-0000-000000000002"), null, null },
                    { new Guid("00000000-0000-0000-0003-000000000005"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("00000000-0000-0000-0001-000000000005"), new Guid("00000000-0000-0000-0000-000000000002"), null, null },
                    { new Guid("00000000-0000-0000-0003-000000000006"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("00000000-0000-0000-0001-000000000006"), new Guid("00000000-0000-0000-0000-000000000002"), null, null },
                    { new Guid("00000000-0000-0000-0003-000000000007"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("00000000-0000-0000-0001-000000000007"), new Guid("00000000-0000-0000-0000-000000000002"), null, null },
                    { new Guid("00000000-0000-0000-0003-000000000008"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("00000000-0000-0000-0001-000000000008"), new Guid("00000000-0000-0000-0000-000000000002"), null, null },
                    { new Guid("00000000-0000-0000-0003-000000000009"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("00000000-0000-0000-0001-000000000009"), new Guid("00000000-0000-0000-0000-000000000002"), null, null },
                    { new Guid("00000000-0000-0000-0003-000000000010"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("00000000-0000-0000-0001-000000000010"), new Guid("00000000-0000-0000-0000-000000000002"), null, null },
                    { new Guid("00000000-0000-0000-0003-000000000011"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("00000000-0000-0000-0001-000000000011"), new Guid("00000000-0000-0000-0000-000000000002"), null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000001"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000002"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000003"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000004"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000005"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000006"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000007"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000008"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000009"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000010"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000011"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0003-000000000001"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0003-000000000002"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0003-000000000003"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0003-000000000004"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0003-000000000005"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0003-000000000006"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0003-000000000007"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0003-000000000008"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0003-000000000009"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0003-000000000010"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0003-000000000011"));
        }
    }
}
