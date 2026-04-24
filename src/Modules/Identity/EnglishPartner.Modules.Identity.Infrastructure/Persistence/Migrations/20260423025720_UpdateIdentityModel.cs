using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishPartner.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateIdentityModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_PermissionId",
                schema: "identity",
                table: "role_permissions",
                column: "PermissionId");

            migrationBuilder.AddForeignKey(
                name: "FK_role_permissions_permissions_PermissionId",
                schema: "identity",
                table: "role_permissions",
                column: "PermissionId",
                principalSchema: "identity",
                principalTable: "permissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_role_permissions_permissions_PermissionId",
                schema: "identity",
                table: "role_permissions");

            migrationBuilder.DropIndex(
                name: "IX_role_permissions_PermissionId",
                schema: "identity",
                table: "role_permissions");
        }
    }
}
