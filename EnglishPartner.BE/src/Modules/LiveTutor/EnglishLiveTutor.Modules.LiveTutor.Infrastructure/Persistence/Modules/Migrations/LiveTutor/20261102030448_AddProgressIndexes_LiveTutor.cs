using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLiveTutor.Modules.LiveTutor.Infrastructure.Persistence.Modules.Migrations.LiveTutor
{
    /// <inheritdoc />
    public partial class AddProgressIndexes_LiveTutor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Conversations_UserId_CreatedAt",
                schema: "livetutor",
                table: "Conversations",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Conversations_UserId_CreatedAt",
                schema: "livetutor",
                table: "Conversations");
        }
    }
}
