using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLiveTutor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGeneratedPronunciationAudioCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GeneratedPronunciationAudios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VoiceName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SpeechSpeed = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    Accent = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, defaultValue: "US"),
                    ModelCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TextSnapshot = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TextHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AudioData = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, defaultValue: "audio/wav"),
                    SampleRate = table.Column<int>(type: "int", nullable: false, defaultValue: 24000),
                    DurationMs = table.Column<int>(type: "int", nullable: false),
                    SystemPromptUsed = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("PK_GeneratedPronunciationAudios", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedPronunciationAudios_Lookup",
                table: "GeneratedPronunciationAudios",
                columns: new[] { "TargetType", "TargetId", "VoiceName", "SpeechSpeed", "Accent", "ModelCode", "TextHash" });

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedPronunciationAudios_TargetType_VoiceName_SpeechSpeed_TargetId",
                table: "GeneratedPronunciationAudios",
                columns: new[] { "TargetType", "VoiceName", "SpeechSpeed", "TargetId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GeneratedPronunciationAudios");
        }
    }
}
