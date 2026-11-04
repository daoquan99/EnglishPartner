using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLiveTutor.Modules.LiveTutor.Infrastructure.Persistence.Modules.Migrations.LiveTutor
{
    /// <inheritdoc />
    public partial class AddCorrectionFeedbackFields_LiveTutor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImprovedText",
                schema: "livetutor",
                table: "Corrections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssuesJson",
                schema: "livetutor",
                table: "Corrections",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "livetutor",
                table: "Corrections",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImprovedText",
                schema: "livetutor",
                table: "Corrections");

            migrationBuilder.DropColumn(
                name: "IssuesJson",
                schema: "livetutor",
                table: "Corrections");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "livetutor",
                table: "Corrections");
        }
    }
}
