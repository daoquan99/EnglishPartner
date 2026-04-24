using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishPartner.Modules.Learning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailToUserLearningProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "learning",
                table: "user_learning_profiles",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                schema: "learning",
                table: "user_learning_profiles");
        }
    }
}
