using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLiveTutor.Modules.Lessons.Infrastructure.Persistence.Modules.Migrations.Lessons
{
    /// <inheritdoc />
    public partial class AddProgressIndexes_Lessons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_LessonAttempts_UserId_CompletedAtUtc",
                schema: "lessons",
                table: "LessonAttempts",
                columns: new[] { "UserId", "CompletedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LessonAttempts_UserId_CompletedAtUtc",
                schema: "lessons",
                table: "LessonAttempts");
        }
    }
}
