using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishPartner.Modules.Learning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiFieldsToActivityLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiFeedback",
                schema: "learning",
                table: "activity_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RawScore",
                schema: "learning",
                table: "activity_logs",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiFeedback",
                schema: "learning",
                table: "activity_logs");

            migrationBuilder.DropColumn(
                name: "RawScore",
                schema: "learning",
                table: "activity_logs");
        }
    }
}
