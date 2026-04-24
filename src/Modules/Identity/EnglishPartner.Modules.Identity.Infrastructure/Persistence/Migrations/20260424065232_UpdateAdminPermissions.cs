using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishPartner.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAdminPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "identity",
                table: "permissions",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "Description", "Module", "Name", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("00000000-0000-0000-0001-000000000014"), "admin", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "admin", "admin", null, null });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "role_permissions",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "PermissionId", "RoleId", "UpdatedAt", "UpdatedBy" },
                values: new object[] { new Guid("00000000-0000-0000-0003-000000000014"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("00000000-0000-0000-0001-000000000014"), new Guid("00000000-0000-0000-0000-000000000002"), null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "identity",
                table: "role_permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0003-000000000014"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000014"));
        }
    }
}
