using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishPartner.Modules.Curriculum.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCurriculumModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "conversations",
                schema: "curriculum",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Scenario = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TopicId = table.Column<Guid>(type: "uuid", nullable: true),
                    GrammarId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "example_sentences",
                schema: "curriculum",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Translation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    AudioUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TopicId = table.Column<Guid>(type: "uuid", nullable: true),
                    VocabularyId = table.Column<Guid>(type: "uuid", nullable: true),
                    GrammarId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_example_sentences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "conversation_lines",
                schema: "curriculum",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Speaker = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Translation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AudioUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversation_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_conversation_lines_conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalSchema: "curriculum",
                        principalTable: "conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_conversation_lines_ConversationId",
                schema: "curriculum",
                table: "conversation_lines",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_conversations_GrammarId",
                schema: "curriculum",
                table: "conversations",
                column: "GrammarId");

            migrationBuilder.CreateIndex(
                name: "IX_conversations_LanguageCode",
                schema: "curriculum",
                table: "conversations",
                column: "LanguageCode");

            migrationBuilder.CreateIndex(
                name: "IX_conversations_TopicId",
                schema: "curriculum",
                table: "conversations",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_example_sentences_GrammarId",
                schema: "curriculum",
                table: "example_sentences",
                column: "GrammarId");

            migrationBuilder.CreateIndex(
                name: "IX_example_sentences_LanguageCode",
                schema: "curriculum",
                table: "example_sentences",
                column: "LanguageCode");

            migrationBuilder.CreateIndex(
                name: "IX_example_sentences_TopicId",
                schema: "curriculum",
                table: "example_sentences",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_example_sentences_VocabularyId",
                schema: "curriculum",
                table: "example_sentences",
                column: "VocabularyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conversation_lines",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "example_sentences",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "conversations",
                schema: "curriculum");
        }
    }
}
