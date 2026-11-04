-- Kiểm tra trước khi chạy app để apply migration chuyển schema (ADR-0006, P4).
-- CHỈ ĐỌC — không sửa dữ liệu. Chạy trong SSMS trên database LingoMint bằng đúng tài khoản app dùng.
USE LingoMint;
GO

-- 1) 49 bảng của app phải đang ở schema dbo (48 bảng sẽ chuyển + IdempotencyRecords ở lại dbo)
SELECT SCHEMA_NAME(t.schema_id) AS [schema], COUNT(*) AS so_bang
FROM sys.tables t
WHERE t.name <> '__EFMigrationsHistory'
GROUP BY SCHEMA_NAME(t.schema_id);

-- 2) 10 migration cũ đã apply (bảng history dbo)
SELECT COUNT(*) AS so_migration_cu FROM dbo.__EFMigrationsHistory;

-- 3) Object ngoài EF (view, proc, function, trigger, synonym) tham chiếu bảng dbo — sẽ HỎNG hoặc chặn TRANSFER
SELECT OBJECT_SCHEMA_NAME(d.referencing_id) AS obj_schema,
       OBJECT_NAME(d.referencing_id)        AS obj_name,
       o.type_desc,
       d.referenced_schema_name, d.referenced_entity_name
FROM sys.sql_expression_dependencies d
JOIN sys.objects o ON o.object_id = d.referencing_id
WHERE ISNULL(d.referenced_schema_name, 'dbo') = 'dbo';
SELECT name, base_object_name FROM sys.synonyms;
-- View WITH SCHEMABINDING làm ALTER SCHEMA TRANSFER thất bại:
SELECT OBJECT_NAME(object_id) AS view_schemabinding FROM sys.sql_modules
WHERE is_schema_bound = 1;

-- 4) Quyền của tài khoản hiện tại (cần db_owner hoặc db_ddladmin)
SELECT USER_NAME() AS db_user,
       IS_ROLEMEMBER('db_owner')    AS is_db_owner,
       IS_ROLEMEMBER('db_ddladmin') AS is_db_ddladmin,
       HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'CREATE SCHEMA') AS can_create_schema;

-- 5) Schema đích chưa tồn tại (nếu đã có — chưa sao, migration dùng IF SCHEMA_ID IS NULL)
SELECT name FROM sys.schemas
WHERE name IN ('identity','catalog','speech','lessons','grammar','vocabulary','livetutor');

-- ===== Sau khi chạy app (kiểm kết quả) =====
-- SELECT SCHEMA_NAME(schema_id) AS [schema], COUNT(*) FROM sys.tables GROUP BY SCHEMA_NAME(schema_id);
--   mong đợi: identity 7 (6 bảng + history), catalog 7, speech 7, lessons 7, grammar 6, vocabulary 17, livetutor 4, dbo 2 (IdempotencyRecords + history cũ)
-- SELECT * FROM [identity].[__EFMigrationsHistory];  -- ... lặp cho từng schema: mỗi bảng 1 dòng MoveToModuleSchema_<X>
