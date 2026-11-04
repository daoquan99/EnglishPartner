-- Backup LingoMint trước khi refactor Modular Monolith (ADR-0006).
-- Chạy trong SSMS hoặc: sqlcmd -S localhost -E -i backup-lingomint.sql
USE master;
GO
DECLARE @db   sysname        = N'LingoMint';
DECLARE @dir  nvarchar(4000) = 'C:\Backups';
-- Nếu báo lỗi quyền ghi: đổi @dir sang thư mục SQL Server service ghi được, ví dụ N'C:\SqlBackup'
DECLARE @file nvarchar(4000) = @dir + N'\' + @db + N'_before_modular_'
                             + FORMAT(SYSDATETIME(), 'yyyyMMdd_HHmmss') + N'.bak';

-- COPY_ONLY: không làm lệch chuỗi backup đang có
BACKUP DATABASE @db
  TO DISK = @file
  WITH COPY_ONLY, INIT, CHECKSUM, STATS = 10,
       NAME = N'LingoMint - before Modular Monolith refactor';
       -- Developer/Standard có thể thêm: , COMPRESSION   (Express không hỗ trợ)

-- Kiểm tra file backup đọc được và checksum hợp lệ
RESTORE VERIFYONLY FROM DISK = @file WITH CHECKSUM;

SELECT @file AS BackupFile;
GO

-- ===== Khôi phục thử sang DB khác (khi cần) =====
-- RESTORE FILELISTONLY FROM DISK = N'<đường dẫn .bak>';   -- lấy LogicalName
-- RESTORE DATABASE LingoMint_Restore FROM DISK = N'<đường dẫn .bak>'
--   WITH MOVE N'<LogicalName data>' TO N'C:\SqlData\LingoMint_Restore.mdf',
--        MOVE N'<LogicalName log>'  TO N'C:\SqlData\LingoMint_Restore_log.ldf',
--        CHECKSUM, STATS = 10;
