using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishPartner.Modules.Notifications.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxIndexesAndPerformanceFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_OutboxMessages",
                schema: "notifications",
                table: "OutboxMessages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InboxMessages",
                schema: "notifications",
                table: "InboxMessages");

            migrationBuilder.RenameTable(
                name: "OutboxMessages",
                schema: "notifications",
                newName: "outbox_messages",
                newSchema: "notifications");

            migrationBuilder.RenameTable(
                name: "InboxMessages",
                schema: "notifications",
                newName: "inbox_messages",
                newSchema: "notifications");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                schema: "notifications",
                table: "outbox_messages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "notifications",
                table: "inbox_messages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_outbox_messages",
                schema: "notifications",
                table: "outbox_messages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_inbox_messages",
                schema: "notifications",
                table: "inbox_messages",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_CreatedAt",
                schema: "notifications",
                table: "notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_OccurredOn",
                schema: "notifications",
                table: "outbox_messages",
                column: "OccurredOn");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedOn",
                schema: "notifications",
                table: "outbox_messages",
                column: "ProcessedOn");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedOn_RetryCount",
                schema: "notifications",
                table: "outbox_messages",
                columns: new[] { "ProcessedOn", "RetryCount" });

            migrationBuilder.CreateIndex(
                name: "IX_inbox_messages_OccurredOn",
                schema: "notifications",
                table: "inbox_messages",
                column: "OccurredOn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_notifications_CreatedAt",
                schema: "notifications",
                table: "notifications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_outbox_messages",
                schema: "notifications",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_OccurredOn",
                schema: "notifications",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_ProcessedOn",
                schema: "notifications",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_ProcessedOn_RetryCount",
                schema: "notifications",
                table: "outbox_messages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_inbox_messages",
                schema: "notifications",
                table: "inbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_inbox_messages_OccurredOn",
                schema: "notifications",
                table: "inbox_messages");

            migrationBuilder.RenameTable(
                name: "outbox_messages",
                schema: "notifications",
                newName: "OutboxMessages",
                newSchema: "notifications");

            migrationBuilder.RenameTable(
                name: "inbox_messages",
                schema: "notifications",
                newName: "InboxMessages",
                newSchema: "notifications");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                schema: "notifications",
                table: "OutboxMessages",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "notifications",
                table: "InboxMessages",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OutboxMessages",
                schema: "notifications",
                table: "OutboxMessages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InboxMessages",
                schema: "notifications",
                table: "InboxMessages",
                column: "Id");
        }
    }
}
