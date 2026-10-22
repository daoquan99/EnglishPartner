using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLiveTutor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFallingWordVocabularyGame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FallingWordGameSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CefrLevel = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    PlayFilter = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DailyPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LearningDate = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    TimeZoneId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TimeLimitSeconds = table.Column<int>(type: "int", nullable: false),
                    RequestedCount = table.Column<int>(type: "int", nullable: false),
                    ActualCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CorrectCount = table.Column<int>(type: "int", nullable: false),
                    IncorrectCount = table.Column<int>(type: "int", nullable: false),
                    TimedOutCount = table.Column<int>(type: "int", nullable: false),
                    AbandonedCount = table.Column<int>(type: "int", nullable: false),
                    BestStreak = table.Column<int>(type: "int", nullable: false),
                    CurrentStreak = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_FallingWordGameSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FallingWordGameRounds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoundOrder = table.Column<int>(type: "int", nullable: false),
                    VocabularyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SenseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DailyItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetWordSnapshot = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NormalizedWordSnapshot = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    MeaningViSnapshot = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CefrLevelSnapshot = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    MaskedPattern = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LetterCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SubmittedText = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ElapsedMs = table.Column<long>(type: "bigint", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FallingWordGameRounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FallingWordGameRounds_FallingWordGameSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "FallingWordGameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FallingWordGameRounds_SenseId_Status",
                table: "FallingWordGameRounds",
                columns: new[] { "SenseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FallingWordGameRounds_SessionId_RoundOrder",
                table: "FallingWordGameRounds",
                columns: new[] { "SessionId", "RoundOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FallingWordGameRounds_Status_ExpiresAtUtc",
                table: "FallingWordGameRounds",
                columns: new[] { "Status", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FallingWordGameSessions_UserId_CreatedAtUtc",
                table: "FallingWordGameSessions",
                columns: new[] { "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FallingWordGameSessions_UserId_Status",
                table: "FallingWordGameSessions",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FallingWordGameRounds");

            migrationBuilder.DropTable(
                name: "FallingWordGameSessions");
        }
    }
}
