using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishPartner.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudyPreferencesAndReminderFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EveningReminderHour",
                schema: "identity",
                table: "user_profiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MorningReminderHour",
                schema: "identity",
                table: "user_profiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UtcOffsetHours",
                schema: "identity",
                table: "user_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EveningReminderHour",
                schema: "identity",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "MorningReminderHour",
                schema: "identity",
                table: "user_profiles");

            migrationBuilder.DropColumn(
                name: "UtcOffsetHours",
                schema: "identity",
                table: "user_profiles");
        }
    }
}
