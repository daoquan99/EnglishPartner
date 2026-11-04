using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLiveTutor.Modules.Vocabulary.Infrastructure.Persistence.Modules.Migrations.Vocabulary
{
    /// <inheritdoc />
    public partial class AddProgressIndexes_Vocabulary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_VocabularyReviewAttempts_UserId_ReviewedAt",
                schema: "vocabulary",
                table: "VocabularyReviewAttempts",
                columns: new[] { "UserId", "ReviewedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VocabularyReviewAttempts_UserId_ReviewedAt",
                schema: "vocabulary",
                table: "VocabularyReviewAttempts");
        }
    }
}
