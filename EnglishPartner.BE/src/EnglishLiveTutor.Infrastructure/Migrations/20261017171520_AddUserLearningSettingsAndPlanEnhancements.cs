using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLiveTutor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLearningSettingsAndPlanEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientRequestId",
                table: "VocabularyReviewAttempts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetCefrLevel",
                table: "DailyVocabularyPlans",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserLearningSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DailyVocabularyTarget = table.Column<int>(type: "int", nullable: false),
                    CefrLevel = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false, defaultValue: "B1"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLearningSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VocabularyReviewAttempts_UserId_ClientRequestId",
                table: "VocabularyReviewAttempts",
                columns: new[] { "UserId", "ClientRequestId" },
                unique: true,
                filter: "[ClientRequestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DailyVocabularyItems_PlanId_VocabularyId",
                table: "DailyVocabularyItems",
                columns: new[] { "PlanId", "VocabularyId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserLearningSettings_UserId",
                table: "UserLearningSettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserLearningSettings");

            migrationBuilder.DropIndex(
                name: "IX_VocabularyReviewAttempts_UserId_ClientRequestId",
                table: "VocabularyReviewAttempts");

            migrationBuilder.DropIndex(
                name: "IX_DailyVocabularyItems_PlanId_VocabularyId",
                table: "DailyVocabularyItems");

            migrationBuilder.DropColumn(
                name: "ClientRequestId",
                table: "VocabularyReviewAttempts");

            migrationBuilder.DropColumn(
                name: "TargetCefrLevel",
                table: "DailyVocabularyPlans");
        }
    }
}
