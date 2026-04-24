using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EnglishPartner.Modules.Curriculum.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedITAndCommunicationTopics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "curriculum",
                table: "topics",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "ImageUrl", "IsActive", "IsDeleted", "LanguageCode", "Level", "SortOrder", "Title", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0002-000000000006"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Vocabulary for IT, hardware, and tech support.", null, true, false, "en", "Intermediate", 6, "Information Technology", null, null },
                    { new Guid("00000000-0000-0000-0002-000000000007"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Coding, software architecture, and agile methodologies.", null, true, false, "en", "Advanced", 7, "Software Development", null, null },
                    { new Guid("00000000-0000-0000-0002-000000000008"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Small talk, expressing opinions, and casual chats.", null, true, false, "en", "Elementary", 8, "Daily Communication", null, null },
                    { new Guid("00000000-0000-0000-0002-000000000009"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Making connections, presenting ideas, and interviews.", null, true, false, "en", "Intermediate", 9, "Professional Networking", null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "curriculum",
                table: "topics",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000006"));

            migrationBuilder.DeleteData(
                schema: "curriculum",
                table: "topics",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000007"));

            migrationBuilder.DeleteData(
                schema: "curriculum",
                table: "topics",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000008"));

            migrationBuilder.DeleteData(
                schema: "curriculum",
                table: "topics",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000009"));
        }
    }
}
