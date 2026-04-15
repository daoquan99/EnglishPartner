using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishPartner.Modules.Learning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_study_sessions_UserId_StartTime",
                schema: "learning",
                table: "study_sessions",
                columns: new[] { "UserId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_study_sessions_UserId_Status",
                schema: "learning",
                table: "study_sessions",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_item_masteries_UserId_NextReviewDate",
                schema: "learning",
                table: "item_masteries",
                columns: new[] { "UserId", "NextReviewDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_study_sessions_UserId_StartTime",
                schema: "learning",
                table: "study_sessions");

            migrationBuilder.DropIndex(
                name: "IX_study_sessions_UserId_Status",
                schema: "learning",
                table: "study_sessions");

            migrationBuilder.DropIndex(
                name: "IX_item_masteries_UserId_NextReviewDate",
                schema: "learning",
                table: "item_masteries");
        }
    }
}
