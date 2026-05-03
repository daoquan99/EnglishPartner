using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EnglishPartner.Modules.Curriculum.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedTopicsData : Migration
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
                    { new Guid("00000000-0000-0000-0002-000000000001"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Basic greetings and introductions.", null, true, false, "en", "Beginner", 1, "Greetings", null, null },
                    { new Guid("00000000-0000-0000-0002-000000000002"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Ordering food, drinks, and describing meals.", null, true, false, "en", "Beginner", 2, "Food & Drink", null, null },
                    { new Guid("00000000-0000-0000-0002-000000000003"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Navigating airports, hotels, and transportation.", null, true, false, "en", "Intermediate", 3, "Travel", null, null },
                    { new Guid("00000000-0000-0000-0002-000000000004"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Professional vocabulary for meetings and emails.", null, true, false, "en", "Advanced", 4, "Business English", null, null },
                    { new Guid("00000000-0000-0000-0002-000000000005"), new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Talking about daily routines and hobbies.", null, true, false, "en", "Elementary", 5, "Everyday Activities", null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "curriculum",
                table: "topics",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000001"));

            migrationBuilder.DeleteData(
                schema: "curriculum",
                table: "topics",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000002"));

            migrationBuilder.DeleteData(
                schema: "curriculum",
                table: "topics",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000003"));

            migrationBuilder.DeleteData(
                schema: "curriculum",
                table: "topics",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000004"));

            migrationBuilder.DeleteData(
                schema: "curriculum",
                table: "topics",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0002-000000000005"));
        }
    }
}
