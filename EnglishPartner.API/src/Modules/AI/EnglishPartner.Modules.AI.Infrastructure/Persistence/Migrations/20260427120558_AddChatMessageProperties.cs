using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishPartner.Modules.AI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessageProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AudioUrl",
                schema: "ai",
                table: "chat_messages",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Order",
                schema: "ai",
                table: "chat_messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Translation",
                schema: "ai",
                table: "chat_messages",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AudioUrl",
                schema: "ai",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "Order",
                schema: "ai",
                table: "chat_messages");

            migrationBuilder.DropColumn(
                name: "Translation",
                schema: "ai",
                table: "chat_messages");
        }
    }
}
