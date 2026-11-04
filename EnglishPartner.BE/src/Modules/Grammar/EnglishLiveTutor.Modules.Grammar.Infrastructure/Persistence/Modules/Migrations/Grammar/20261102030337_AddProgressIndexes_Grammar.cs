using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLiveTutor.Modules.Grammar.Infrastructure.Persistence.Modules.Migrations.Grammar
{
    /// <inheritdoc />
    public partial class AddProgressIndexes_Grammar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_GrammarAttempts_UserId_CompletedAtUtc",
                schema: "grammar",
                table: "GrammarAttempts",
                columns: new[] { "UserId", "CompletedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GrammarAttempts_UserId_CompletedAtUtc",
                schema: "grammar",
                table: "GrammarAttempts");
        }
    }
}
