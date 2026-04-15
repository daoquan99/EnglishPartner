using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EnglishPartner.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000009"),
                columns: new[] { "Code", "Module", "Name" },
                values: new object[] { "learning.admin.write", "learning", "Write Admin" });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000010"),
                columns: new[] { "Code", "Name" },
                values: new object[] { "identity.users.manage", "Manage Users" });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000011"),
                columns: new[] { "Code", "Module", "Name" },
                values: new object[] { "identity.roles.manage", "identity", "Manage Roles" });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "permissions",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "Description", "Module", "Name", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0001-000000000012"), "ai.logs.read", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "ai", "Read Logs", null, null },
                    { new Guid("00000000-0000-0000-0001-000000000013"), "notifications.admin", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "notifications", "notifications.admin", null, null }
                });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "role_permissions",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0003-000000000012"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("00000000-0000-0000-0001-000000000012"), new Guid("00000000-0000-0000-0000-000000000002"), null, null },
                    { new Guid("00000000-0000-0000-0003-000000000013"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("00000000-0000-0000-0001-000000000013"), new Guid("00000000-0000-0000-0000-000000000002"), null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_CreatedAt",
                schema: "identity",
                table: "users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_users_IsActive",
                schema: "identity",
                table: "users",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_CreatedAt",
                schema: "identity",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_IsActive",
                schema: "identity",
                table: "users");

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000012"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000013"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0003-000000000012"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0003-000000000013"));

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000009"),
                columns: new[] { "Code", "Module", "Name" },
                values: new object[] { "identity.users.manage", "identity", "Manage Users" });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000010"),
                columns: new[] { "Code", "Name" },
                values: new object[] { "identity.roles.manage", "Manage Roles" });

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000011"),
                columns: new[] { "Code", "Module", "Name" },
                values: new object[] { "ai.logs.read", "ai", "Read Logs" });
        }
    }
}
