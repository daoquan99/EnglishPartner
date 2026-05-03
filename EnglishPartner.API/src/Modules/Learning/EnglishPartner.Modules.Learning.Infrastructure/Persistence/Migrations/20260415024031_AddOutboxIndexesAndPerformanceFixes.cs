using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishPartner.Modules.Learning.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxIndexesAndPerformanceFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_OutboxMessages",
                schema: "learning",
                table: "OutboxMessages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InboxMessages",
                schema: "learning",
                table: "InboxMessages");

            migrationBuilder.RenameTable(
                name: "OutboxMessages",
                schema: "learning",
                newName: "outbox_messages",
                newSchema: "learning");

            migrationBuilder.RenameTable(
                name: "InboxMessages",
                schema: "learning",
                newName: "inbox_messages",
                newSchema: "learning");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                schema: "learning",
                table: "outbox_messages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "learning",
                table: "inbox_messages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_outbox_messages",
                schema: "learning",
                table: "outbox_messages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_inbox_messages",
                schema: "learning",
                table: "inbox_messages",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_OccurredOn",
                schema: "learning",
                table: "outbox_messages",
                column: "OccurredOn");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedOn",
                schema: "learning",
                table: "outbox_messages",
                column: "ProcessedOn");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedOn_RetryCount",
                schema: "learning",
                table: "outbox_messages",
                columns: new[] { "ProcessedOn", "RetryCount" });

            migrationBuilder.CreateIndex(
                name: "IX_inbox_messages_OccurredOn",
                schema: "learning",
                table: "inbox_messages",
                column: "OccurredOn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_outbox_messages",
                schema: "learning",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_OccurredOn",
                schema: "learning",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_ProcessedOn",
                schema: "learning",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_ProcessedOn_RetryCount",
                schema: "learning",
                table: "outbox_messages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_inbox_messages",
                schema: "learning",
                table: "inbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_inbox_messages_OccurredOn",
                schema: "learning",
                table: "inbox_messages");

            migrationBuilder.RenameTable(
                name: "outbox_messages",
                schema: "learning",
                newName: "OutboxMessages",
                newSchema: "learning");

            migrationBuilder.RenameTable(
                name: "inbox_messages",
                schema: "learning",
                newName: "InboxMessages",
                newSchema: "learning");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                schema: "learning",
                table: "OutboxMessages",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "learning",
                table: "InboxMessages",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OutboxMessages",
                schema: "learning",
                table: "OutboxMessages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InboxMessages",
                schema: "learning",
                table: "InboxMessages",
                column: "Id");
        }
    }
}
