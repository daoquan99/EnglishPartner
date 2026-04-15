using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EnglishPartner.Modules.Curriculum.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "curriculum");

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
                    IsAiGenerated = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
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
                    LanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    AudioUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TopicId = table.Column<Guid>(type: "uuid", nullable: true),
                    VocabularyId = table.Column<Guid>(type: "uuid", nullable: true),
                    GrammarId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsAiGenerated = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
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
                name: "exercises",
                schema: "curriculum",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Instruction = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TopicId = table.Column<Guid>(type: "uuid", nullable: true),
                    VocabularyId = table.Column<Guid>(type: "uuid", nullable: true),
                    GrammarId = table.Column<Guid>(type: "uuid", nullable: true),
                    Content = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    CorrectAnswer = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Explanation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    XpReward = table.Column<int>(type: "integer", nullable: false),
                    IsAiGenerated = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
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
                    table.PrimaryKey("PK_exercises", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "grammars",
                schema: "curriculum",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Explanation = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    Formula = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TopicId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsAiGenerated = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
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
                    table.PrimaryKey("PK_grammars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                schema: "curriculum",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    OccurredOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "languages",
                schema: "curriculum",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NativeName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_languages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "curriculum",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    OccurredOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "topics",
                schema: "curriculum",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_topics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "vocabularies",
                schema: "curriculum",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Word = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Definition = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Pronunciation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PhoneticTranscription = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PartOfSpeech = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AudioUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TopicId = table.Column<Guid>(type: "uuid", nullable: true),
                    Synonyms = table.Column<List<string>>(type: "text[]", nullable: false),
                    IsAiGenerated = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
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
                    table.PrimaryKey("PK_vocabularies", x => x.Id);
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

            migrationBuilder.CreateTable(
                name: "example_sentence_translations",
                schema: "curriculum",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExampleSentenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TranslatedText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_example_sentence_translations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_example_sentence_translations_example_sentences_ExampleSent~",
                        column: x => x.ExampleSentenceId,
                        principalSchema: "curriculum",
                        principalTable: "example_sentences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exercise_options",
                schema: "curriculum",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercise_options", x => x.Id);
                    table.ForeignKey(
                        name: "FK_exercise_options_exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalSchema: "curriculum",
                        principalTable: "exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "grammar_examples",
                schema: "curriculum",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GrammarId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sentence = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grammar_examples", x => x.Id);
                    table.ForeignKey(
                        name: "FK_grammar_examples_grammars_GrammarId",
                        column: x => x.GrammarId,
                        principalSchema: "curriculum",
                        principalTable: "grammars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "conversation_line_translations",
                schema: "curriculum",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TranslatedText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversation_line_translations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_conversation_line_translations_conversation_lines_Conversat~",
                        column: x => x.ConversationLineId,
                        principalSchema: "curriculum",
                        principalTable: "conversation_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "grammar_example_translations",
                schema: "curriculum",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GrammarExampleId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TranslatedText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grammar_example_translations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_grammar_example_translations_grammar_examples_GrammarExampl~",
                        column: x => x.GrammarExampleId,
                        principalSchema: "curriculum",
                        principalTable: "grammar_examples",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "curriculum",
                table: "languages",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "IsActive", "IsDeleted", "Name", "NativeName", "SortOrder", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0001-000000000001"), "en", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, false, "English", "English", 1, null, null },
                    { new Guid("00000000-0000-0000-0001-000000000002"), "vi", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, false, "Vietnamese", "Tiếng Việt", 2, null, null },
                    { new Guid("00000000-0000-0000-0001-000000000003"), "zh", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, false, "Chinese", "中文", 3, null, null },
                    { new Guid("00000000-0000-0000-0001-000000000004"), "ja", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, false, "Japanese", "日本語", 4, null, null },
                    { new Guid("00000000-0000-0000-0001-000000000005"), "ko", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, false, "Korean", "한국어", 5, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_conversation_line_translations_ConversationLineId_LanguageC~",
                schema: "curriculum",
                table: "conversation_line_translations",
                columns: new[] { "ConversationLineId", "LanguageCode" },
                unique: true);

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
                name: "IX_example_sentence_translations_ExampleSentenceId_LanguageCode",
                schema: "curriculum",
                table: "example_sentence_translations",
                columns: new[] { "ExampleSentenceId", "LanguageCode" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_exercise_options_ExerciseId",
                schema: "curriculum",
                table: "exercise_options",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_exercises_GrammarId",
                schema: "curriculum",
                table: "exercises",
                column: "GrammarId");

            migrationBuilder.CreateIndex(
                name: "IX_exercises_LanguageCode",
                schema: "curriculum",
                table: "exercises",
                column: "LanguageCode");

            migrationBuilder.CreateIndex(
                name: "IX_exercises_TopicId",
                schema: "curriculum",
                table: "exercises",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_exercises_VocabularyId",
                schema: "curriculum",
                table: "exercises",
                column: "VocabularyId");

            migrationBuilder.CreateIndex(
                name: "IX_grammar_example_translations_GrammarExampleId_LanguageCode",
                schema: "curriculum",
                table: "grammar_example_translations",
                columns: new[] { "GrammarExampleId", "LanguageCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_grammar_examples_GrammarId",
                schema: "curriculum",
                table: "grammar_examples",
                column: "GrammarId");

            migrationBuilder.CreateIndex(
                name: "IX_grammars_LanguageCode",
                schema: "curriculum",
                table: "grammars",
                column: "LanguageCode");

            migrationBuilder.CreateIndex(
                name: "IX_grammars_TopicId",
                schema: "curriculum",
                table: "grammars",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_languages_Code",
                schema: "curriculum",
                table: "languages",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_topics_LanguageCode",
                schema: "curriculum",
                table: "topics",
                column: "LanguageCode");

            migrationBuilder.CreateIndex(
                name: "IX_vocabularies_LanguageCode",
                schema: "curriculum",
                table: "vocabularies",
                column: "LanguageCode");

            migrationBuilder.CreateIndex(
                name: "IX_vocabularies_TopicId",
                schema: "curriculum",
                table: "vocabularies",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_vocabularies_Word_LanguageCode",
                schema: "curriculum",
                table: "vocabularies",
                columns: new[] { "Word", "LanguageCode" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conversation_line_translations",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "example_sentence_translations",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "exercise_options",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "grammar_example_translations",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "languages",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "topics",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "vocabularies",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "conversation_lines",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "example_sentences",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "exercises",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "grammar_examples",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "conversations",
                schema: "curriculum");

            migrationBuilder.DropTable(
                name: "grammars",
                schema: "curriculum");
        }
    }
}
