using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishPartner.Modules.Curriculum.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxIndexesAndPerformanceFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_OutboxMessages",
                schema: "curriculum",
                table: "OutboxMessages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InboxMessages",
                schema: "curriculum",
                table: "InboxMessages");

            migrationBuilder.RenameTable(
                name: "OutboxMessages",
                schema: "curriculum",
                newName: "outbox_messages",
                newSchema: "curriculum");

            migrationBuilder.RenameTable(
                name: "InboxMessages",
                schema: "curriculum",
                newName: "inbox_messages",
                newSchema: "curriculum");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                schema: "curriculum",
                table: "outbox_messages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "curriculum",
                table: "inbox_messages",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_outbox_messages",
                schema: "curriculum",
                table: "outbox_messages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_inbox_messages",
                schema: "curriculum",
                table: "inbox_messages",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_OccurredOn",
                schema: "curriculum",
                table: "outbox_messages",
                column: "OccurredOn");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedOn",
                schema: "curriculum",
                table: "outbox_messages",
                column: "ProcessedOn");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_ProcessedOn_RetryCount",
                schema: "curriculum",
                table: "outbox_messages",
                columns: new[] { "ProcessedOn", "RetryCount" });

            migrationBuilder.CreateIndex(
                name: "IX_inbox_messages_OccurredOn",
                schema: "curriculum",
                table: "inbox_messages",
                column: "OccurredOn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_outbox_messages",
                schema: "curriculum",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_OccurredOn",
                schema: "curriculum",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_ProcessedOn",
                schema: "curriculum",
                table: "outbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_outbox_messages_ProcessedOn_RetryCount",
                schema: "curriculum",
                table: "outbox_messages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_inbox_messages",
                schema: "curriculum",
                table: "inbox_messages");

            migrationBuilder.DropIndex(
                name: "IX_inbox_messages_OccurredOn",
                schema: "curriculum",
                table: "inbox_messages");

            migrationBuilder.RenameTable(
                name: "outbox_messages",
                schema: "curriculum",
                newName: "OutboxMessages",
                newSchema: "curriculum");

            migrationBuilder.RenameTable(
                name: "inbox_messages",
                schema: "curriculum",
                newName: "InboxMessages",
                newSchema: "curriculum");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                schema: "curriculum",
                table: "OutboxMessages",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "curriculum",
                table: "InboxMessages",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddPrimaryKey(
                name: "PK_OutboxMessages",
                schema: "curriculum",
                table: "OutboxMessages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InboxMessages",
                schema: "curriculum",
                table: "InboxMessages",
                column: "Id");
        }
    }
}
