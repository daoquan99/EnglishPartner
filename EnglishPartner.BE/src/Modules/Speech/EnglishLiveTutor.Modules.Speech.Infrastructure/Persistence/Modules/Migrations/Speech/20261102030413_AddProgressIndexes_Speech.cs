using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLiveTutor.Modules.Speech.Infrastructure.Persistence.Modules.Migrations.Speech
{
    /// <inheritdoc />
    public partial class AddProgressIndexes_Speech : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PronunciationAssessments_UserId_CreatedAt",
                schema: "speech",
                table: "PronunciationAssessments",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PronunciationAssessments_UserId_CreatedAt",
                schema: "speech",
                table: "PronunciationAssessments");
        }
    }
}
