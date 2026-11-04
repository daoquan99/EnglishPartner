# P3 fix 1 — siết test kiến trúc sau khi tách project (chỉ sửa test)

Chỉ sửa file trong `tests/EnglishLiveTutor.Domain.Tests/`. Không đổi `src/`.

1. `Architecture/ModuleBoundaryTests.cs` ~72-76 `EveryDomainAndFeatureFolderIsMapped`: duyệt `src/Modules/*/*/Features/*` và `src/Modules/*/*/Domain/*` (và `src/EnglishLiveTutor.Domain/*`); thư mục gốc không tồn tại → **fail** (không trả rỗng); assert có ít nhất 1 thư mục được kiểm.
2. `AiCommandsUnitOfWorkTests.cs` ~163: quét `ApplicationAssemblies.All` thay cho một assembly; giữ nguyên ý nghĩa assert (không handler nào phụ thuộc factory cũ); assert số handler được quét ≥ số trước P3 (ghi số).
3. Test mới `Architecture/ApplicationAssembliesTests.cs`: mọi `*.csproj` dưới `src/Modules/**` và `src/EnglishLiveTutor.SharedKernel` có tên assembly nằm trong `ApplicationAssemblies.All` (đọc `AssemblyName` hoặc tên file csproj).
4. `Architecture/ProjectReferenceTests.cs`: thêm test duyệt **mọi** csproj dưới `src/Modules/`, suy module từ đường dẫn, và kiểm `ProjectReference` ⊆ {SharedKernel, Domain, Contracts của chính module, Contracts của module trong `ModuleMap.AllowedDependencies` + Identity}; project Contracts ⊆ {SharedKernel, Domain, Contracts của module trong DAG}. Giữ test cố định hiện có.

Chạy build `--no-restore` + toàn bộ test; report số test trước/sau.
