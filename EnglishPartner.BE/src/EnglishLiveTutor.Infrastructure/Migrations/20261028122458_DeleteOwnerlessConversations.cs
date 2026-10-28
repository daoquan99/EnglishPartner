// Migration này xóa toàn bộ dữ liệu Corrections, Messages, Conversations không có chủ sở hữu (ADR-0002).
// CẢNH BÁO: Thao tác này không thể hoàn tác (irreversible)!
// Phải backup database trước khi chạy trên môi trường thật.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishLiveTutor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DeleteOwnerlessConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Xóa bảng con trước để tránh ràng buộc khóa ngoại
            migrationBuilder.Sql("DELETE FROM [Corrections];");
            migrationBuilder.Sql("DELETE FROM [Messages];");
            migrationBuilder.Sql("DELETE FROM [Conversations];");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không thể hoàn tác việc xóa dữ liệu hội thoại cũ đã bị xóa.
        }
    }
}
