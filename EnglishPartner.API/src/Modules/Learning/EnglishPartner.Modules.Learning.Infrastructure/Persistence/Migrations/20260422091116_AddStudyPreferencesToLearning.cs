using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishPartner.Modules.Learning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudyPreferencesToLearning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "daily_goal_minutes",
                schema: "learning",
                table: "user_learning_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 15);

            migrationBuilder.AddColumn<int>(
                name: "evening_reminder_hour",
                schema: "learning",
                table: "user_learning_profiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "morning_reminder_hour",
                schema: "learning",
                table: "user_learning_profiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "utc_offset_hours",
                schema: "learning",
                table: "user_learning_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "daily_goal_minutes",
                schema: "learning",
                table: "user_learning_profiles");

            migrationBuilder.DropColumn(
                name: "evening_reminder_hour",
                schema: "learning",
                table: "user_learning_profiles");

            migrationBuilder.DropColumn(
                name: "morning_reminder_hour",
                schema: "learning",
                table: "user_learning_profiles");

            migrationBuilder.DropColumn(
                name: "utc_offset_hours",
                schema: "learning",
                table: "user_learning_profiles");
        }
    }
}
