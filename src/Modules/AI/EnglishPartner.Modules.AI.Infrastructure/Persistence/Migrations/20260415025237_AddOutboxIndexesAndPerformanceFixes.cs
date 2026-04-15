using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishPartner.Modules.AI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxIndexesAndPerformanceFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_OutboxMessages",
                schema: "ai",
                table: "OutboxMessages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InboxMessages",
                schema: "ai",
                table: "InboxMessages");

            migrationBuilder.RenameTable(
                name: "OutboxMessages",
                schema: "ai",
                newName: "outbox_messages",
                newSchema: "ai");

            migrationBuilder.RenameTable(
                name: "InboxMessages",
                schema: "ai",
                newName: "inbox_messages",
                newSchema: "ai");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                schema: "ai",
                table: "outbox_messages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "ai",
                table: "inbox_messages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_outbox_messages",
                schema: "ai",
                table: "outbox_messages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_inbox_messages",
                schema: "ai",
                table: "inbox_messages",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_OccurredOn",
                schema: "ai",
                table: "outbox_messages",
                column: "OccurredOn");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedOn",
                schema: "ai",
                table: "outbox_messages",
                column: "ProcessedOn");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedOn_RetryCount",
                schema: "ai",
                table: "outbox_messages",
                columns: new[] { "ProcessedOn", "RetryCount" });

            migrationBuilder.CreateIndex(
                name: "IX_inbox_messages_OccurredOn",
                schema: "ai",
                table: "inbox_messages",
                column: "OccurredOn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_outbox_messages",
                schema: "ai",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_OccurredOn",
                schema: "ai",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_ProcessedOn",
                schema: "ai",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_ProcessedOn_RetryCount",
                schema: "ai",
                table: "outbox_messages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_inbox_messages",
                schema: "ai",
                table: "inbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_inbox_messages_OccurredOn",
                schema: "ai",
                table: "inbox_messages");

            migrationBuilder.RenameTable(
                name: "outbox_messages",
                schema: "ai",
                newName: "OutboxMessages",
                newSchema: "ai");

            migrationBuilder.RenameTable(
                name: "inbox_messages",
                schema: "ai",
                newName: "InboxMessages",
                newSchema: "ai");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                schema: "ai",
                table: "OutboxMessages",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "ai",
                table: "InboxMessages",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OutboxMessages",
                schema: "ai",
                table: "OutboxMessages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InboxMessages",
                schema: "ai",
                table: "InboxMessages",
                column: "Id");
        }
    }
}
