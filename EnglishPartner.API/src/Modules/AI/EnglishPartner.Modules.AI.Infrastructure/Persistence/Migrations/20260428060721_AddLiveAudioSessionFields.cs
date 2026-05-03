using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishPartner.Modules.AI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLiveAudioSessionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SessionType",
                schema: "ai",
                table: "chat_sessions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "TextChat");

            migrationBuilder.AddColumn<string>(
                name: "VoiceGender",
                schema: "ai",
                table: "chat_sessions",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoiceLanguageCode",
                schema: "ai",
                table: "chat_sessions",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoiceName",
                schema: "ai",
                table: "chat_sessions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SessionType",
                schema: "ai",
                table: "chat_sessions");

            migrationBuilder.DropColumn(
                name: "VoiceGender",
                schema: "ai",
                table: "chat_sessions");

            migrationBuilder.DropColumn(
                name: "VoiceLanguageCode",
                schema: "ai",
                table: "chat_sessions");

            migrationBuilder.DropColumn(
                name: "VoiceName",
                schema: "ai",
                table: "chat_sessions");
        }
    }
}
