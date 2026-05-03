using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishPartner.Modules.Learning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixLearningActivityLogBaseClass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "learning",
                table: "activity_logs");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "learning",
                table: "activity_logs");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "learning",
                table: "activity_logs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "learning",
                table: "activity_logs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "learning",
                table: "activity_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                schema: "learning",
                table: "activity_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "learning",
                table: "activity_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "learning",
                table: "activity_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
