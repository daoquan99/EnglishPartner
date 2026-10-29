using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLiveTutor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGrammarRoadmap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GrammarSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CefrLevel = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    TitleVi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SummaryVi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TheoryJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TheoryVersion = table.Column<int>(type: "int", nullable: false),
                    TheoryEditedByAdmin = table.Column<bool>(type: "bit", nullable: false),
                    IsLevelReview = table.Column<bool>(type: "bit", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_GrammarSteps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GrammarAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CefrLevel = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    ExerciseIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Score100 = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
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
                    table.PrimaryKey("PK_GrammarAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GrammarAttempts_GrammarSteps_StepId",
                        column: x => x.StepId,
                        principalTable: "GrammarSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GrammarExercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Difficulty = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Focus = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContentJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GradingDefinitionJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxPoints = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    GradingMode = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    OwnerUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AuthorUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Visibility = table.Column<int>(type: "int", nullable: false),
                    Moderation = table.Column<int>(type: "int", nullable: false),
                    RejectReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsCore = table.Column<bool>(type: "bit", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
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
                    table.PrimaryKey("PK_GrammarExercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GrammarExercises_GrammarSteps_StepId",
                        column: x => x.StepId,
                        principalTable: "GrammarSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserGrammarStepProgress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BestScore100 = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    PassedBy = table.Column<int>(type: "int", nullable: true),
                    UnlockedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PassedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_UserGrammarStepProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserGrammarStepProgress_GrammarSteps_StepId",
                        column: x => x.StepId,
                        principalTable: "GrammarSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GrammarAttemptAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AnswerPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Score100 = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    PointsEarned = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    MaxPoints = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    FeedbackJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrammarAttemptAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GrammarAttemptAnswers_GrammarAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "GrammarAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GrammarAttemptAnswers_AttemptId_ExerciseId",
                table: "GrammarAttemptAnswers",
                columns: new[] { "AttemptId", "ExerciseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GrammarAttempts_StepId",
                table: "GrammarAttempts",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_GrammarAttempts_UserId_StartedAtUtc",
                table: "GrammarAttempts",
                columns: new[] { "UserId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GrammarExercises_OwnerUserId_StepId",
                table: "GrammarExercises",
                columns: new[] { "OwnerUserId", "StepId" });

            migrationBuilder.CreateIndex(
                name: "IX_GrammarExercises_StepId_ContentHash",
                table: "GrammarExercises",
                columns: new[] { "StepId", "ContentHash" });

            migrationBuilder.CreateIndex(
                name: "IX_GrammarExercises_StepId_Key",
                table: "GrammarExercises",
                columns: new[] { "StepId", "Key" },
                unique: true,
                filter: "[Key] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GrammarExercises_StepId_Source_Moderation_IsDeleted",
                table: "GrammarExercises",
                columns: new[] { "StepId", "Source", "Moderation", "IsDeleted" })
                .Annotation("SqlServer:Include", new[] { "IsCore", "OwnerUserId", "Difficulty", "Key", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_GrammarSteps_CefrLevel_Order",
                table: "GrammarSteps",
                columns: new[] { "CefrLevel", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GrammarSteps_Code",
                table: "GrammarSteps",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserGrammarStepProgress_StepId",
                table: "UserGrammarStepProgress",
                column: "StepId");

            migrationBuilder.CreateIndex(
                name: "IX_UserGrammarStepProgress_UserId_StepId",
                table: "UserGrammarStepProgress",
                columns: new[] { "UserId", "StepId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GrammarAttemptAnswers");

            migrationBuilder.DropTable(
                name: "GrammarExercises");

            migrationBuilder.DropTable(
                name: "UserGrammarStepProgress");

            migrationBuilder.DropTable(
                name: "GrammarAttempts");

            migrationBuilder.DropTable(
                name: "GrammarSteps");
        }
    }
}
