using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLiveTutor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonAndExercises : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicableExerciseTypes",
                table: "ScoringRubrics",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssessmentScope",
                table: "ScoringRubrics",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Pronunciation");

            migrationBuilder.AddColumn<string>(
                name: "CefrLevel",
                table: "ScoringRubrics",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "All");

            migrationBuilder.AddColumn<int>(
                name: "ExerciseType",
                table: "ScoringRubrics",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubQuestionKind",
                table: "ScoringRubrics",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptorLevel0",
                table: "ScoringRubricCriteria",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DescriptorLevel1",
                table: "ScoringRubricCriteria",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DescriptorLevel2",
                table: "ScoringRubricCriteria",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DescriptorLevel3",
                table: "ScoringRubricCriteria",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DescriptorLevel4",
                table: "ScoringRubricCriteria",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "LessonAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PinnedLessonVersion = table.Column<int>(type: "int", nullable: false),
                    AttemptRevision = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CurrentSubtotalPoints = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    TotalMaxPoints = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    FinalScore100 = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
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
                    table.PrimaryKey("PK_LessonAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lessons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CefrLevel = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Topic = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LearningObjectivesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetVocabularyJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetGrammarJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    ParentLessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_Lessons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubQuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GradingMode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    MaxPoints = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Score100 = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    PointsEarned = table.Column<decimal>(type: "decimal(6,2)", nullable: true),
                    AnswerPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GradingResultJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LeaseExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    GradedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseAnswers_LessonAttempts_AttemptId",
                        column: x => x.AttemptId,
                        principalTable: "LessonAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Exercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    MaxPoints = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    GradingMode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SkillTagsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GradingDefinitionJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Exercises_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseAnswers_AttemptId_ExerciseId_SubQuestionId",
                table: "ExerciseAnswers",
                columns: new[] { "AttemptId", "ExerciseId", "SubQuestionId" },
                unique: true,
                filter: "[SubQuestionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseAnswers_Status",
                table: "ExerciseAnswers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_LessonId_Key",
                table: "Exercises",
                columns: new[] { "LessonId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_LessonId_Order",
                table: "Exercises",
                columns: new[] { "LessonId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_LessonAttempts_Status",
                table: "LessonAttempts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LessonAttempts_UserId_LessonId",
                table: "LessonAttempts",
                columns: new[] { "UserId", "LessonId" });

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_CefrLevel",
                table: "Lessons",
                column: "CefrLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_Id_Version",
                table: "Lessons",
                columns: new[] { "Id", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_Status",
                table: "Lessons",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_Topic",
                table: "Lessons",
                column: "Topic");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExerciseAnswers");

            migrationBuilder.DropTable(
                name: "Exercises");

            migrationBuilder.DropTable(
                name: "LessonAttempts");

            migrationBuilder.DropTable(
                name: "Lessons");

            migrationBuilder.DropColumn(
                name: "ApplicableExerciseTypes",
                table: "ScoringRubrics");

            migrationBuilder.DropColumn(
                name: "AssessmentScope",
                table: "ScoringRubrics");

            migrationBuilder.DropColumn(
                name: "CefrLevel",
                table: "ScoringRubrics");

            migrationBuilder.DropColumn(
                name: "ExerciseType",
                table: "ScoringRubrics");

            migrationBuilder.DropColumn(
                name: "SubQuestionKind",
                table: "ScoringRubrics");

            migrationBuilder.DropColumn(
                name: "DescriptorLevel0",
                table: "ScoringRubricCriteria");

            migrationBuilder.DropColumn(
                name: "DescriptorLevel1",
                table: "ScoringRubricCriteria");

            migrationBuilder.DropColumn(
                name: "DescriptorLevel2",
                table: "ScoringRubricCriteria");

            migrationBuilder.DropColumn(
                name: "DescriptorLevel3",
                table: "ScoringRubricCriteria");

            migrationBuilder.DropColumn(
                name: "DescriptorLevel4",
                table: "ScoringRubricCriteria");
        }
    }
}
