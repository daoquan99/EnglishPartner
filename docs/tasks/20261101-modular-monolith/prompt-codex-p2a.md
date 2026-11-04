# Việc giao cho Codex: P2a — Shared helper, enum Contracts, Catalog reader, cập nhật DAG

## Bối cảnh cần đọc (session mới — đọc kỹ)

- `AGENTS.md` (gốc, luật 13–15), `EnglishPartner.BE/AGENTS.md`
- `docs/adr/0006-modular-monolith.md`, skill `EnglishPartner.BE/.agents/skills/modular-monolith/SKILL.md`
- Thiết kế P2 của orchestrator: `docs/tasks/20261101-modular-monolith/plan-p2.md` — gói này làm **D1, D2, D3, D4** và cập nhật DAG.
- Trạng thái: P1 xong. Có 7 interface `src/EnglishLiveTutor.Application/Modules/<M>/I<M>DbContext.cs` (cùng instance `AppDbContext` scoped), `IModuleDbContextFactory<T>`. Architecture test ở `tests/EnglishLiveTutor.Domain.Tests/Architecture/` (`ModuleMap.cs`, scanner, `module-boundary-allowlist.txt` — chỉ được bớt dòng; `MODULE_ALLOWLIST_WRITE=1` chỉ xóa dòng stale).
- Baseline: build 0 lỗi, `dotnet test` 570 pass / 0 fail. Môi trường không có mạng: dùng `--no-restore`.

## Yêu cầu

1. **DAG** — cập nhật đồng bộ:
   - `tests/.../Architecture/ModuleMap.cs` `AllowedDependencies`: mọi module → `Identity`; Speech → Catalog; Lessons → Speech, Catalog; Grammar → Lessons, Speech; Vocabulary → Speech, Catalog; LiveTutor → Catalog. (Bỏ Grammar → Vocabulary.)
   - `docs/adr/0006-modular-monolith.md`: cột "Phụ thuộc Contracts của" và câu về Identity cho khớp (Identity: mọi module được dùng `Identity.Contracts`).
   - `EnglishPartner.BE/.agents/skills/modular-monolith/SKILL.md` dòng "Allowed dependencies (DAG)" cho khớp.
2. **D1**: tạo `src/EnglishLiveTutor.Application/Common/Persistence/DbUpdateExceptionClassifier.cs` (`public static class`, namespace `EnglishLiveTutor.Application.Common.Persistence`) chứa **nguyên văn** thân hàm `IsUniqueConstraintViolation(DbUpdateException)` từ `Features/Vocabulary/VocabularyGenerationWriter.cs`. Xóa hàm cũ, sửa mọi caller (Grammar, Vocabulary, nơi khác nếu có) gọi hàm mới.
3. **D2**: chuyển `ExerciseType`, `GradingMode`, `SubQuestionKind` từ `src/EnglishLiveTutor.Domain/Lessons/Enums/` sang `src/EnglishLiveTutor.Domain/Lessons/Contracts/` (git mv), đổi namespace thành `EnglishLiveTutor.Domain.Lessons.Contracts`; giữ nguyên tên thành viên, giá trị số, attribute. Sửa `using` ở mọi file (Domain, Application, Infrastructure, Api, tests). `LessonStatus` giữ nguyên chỗ cũ. Không sửa migration; nếu file trong `Migrations/` tham chiếu namespace cũ → dừng và báo.
4. **D3**: chuyển thư mục `Features/Pronunciation/Queries/GetPronunciationModels/` và `Features/Vocabulary/Queries/GetVocabularyGenerationModels/` vào `Features/AiModels/Queries/` (git mv, đổi namespace theo thư mục mới). Chuyển phần map endpoint tương ứng từ `src/EnglishLiveTutor.Api/Endpoints/PronunciationEndpoints.cs` và `VocabularyEndpoints.cs` sang `AiModelEndpoints.cs`: **giữ y nguyên** route đầy đủ (tính cả prefix của group), HTTP method, `RequireAuthorization`/policy, rate limit, filter, `WithName`/tags nếu có, kiểu kết quả. Nếu route nằm trong group có cấu hình chung (auth/rate limit/prefix), tạo group tương đương trong `AiModelEndpoints.cs` với cùng cấu hình; liệt kê route trước/sau trong report.
5. **D4**: tạo `src/EnglishLiveTutor.Application/Features/AiModels/Contracts/IAiModelCatalogReader.cs` + `AiModelInfo.cs` (record), impl `Features/AiModels/Services/AiModelCatalogReader.cs` dùng `ICatalogDbContext` (scoped), đăng ký scoped trong `src/EnglishLiveTutor.Application/DependencyInjection.cs`. Method: `Task<AiModelInfo?> FindByModelCodeAsync(string modelCode, CancellationToken ct = default)` với query **giống hệt** caller hiện tại (`AsNoTracking`, so sánh `ModelCode ==`). `AiModelInfo` chứa đúng các member mà 3 caller dùng (đọc code để liệt kê) và `EffectiveModelCode` = giá trị `GetEffectiveModelCode()` tại thời điểm map. Sửa 3 caller dùng reader:
   - `Features/Lessons/Commands/ImportOperations/GenerateLessonExercisesCommand.cs`
   - `Features/Pronunciation/Commands/AssessPronunciation/AssessPronunciationCommandHandler.cs` (chỉ phần đọc `AiModelCatalog`; phần đọc Vocabulary để P2c)
   - `Features/Vocabulary/Commands/GenerateVocabulary/GenerateVocabularyCommandHandler.cs`
   Giữ nguyên message lỗi, thứ tự kiểm tra. Nếu sau khi bỏ `AiModelCatalog`, file chỉ còn dùng `DbSet` module của chính nó → chuyển sang `I<Module>DbContext`; nếu còn dùng module khác → giữ `IAppDbContext`.
6. Thu nhỏ allowlist bằng `MODULE_ALLOWLIST_WRITE=1`. Được phép allowlist có dòng mới **chỉ** nếu do D3/D4 chuyển file và test báo vi phạm mới — khi đó **không** thêm vào allowlist mà sửa cho hết vi phạm (dùng Contracts), hoặc dừng và báo.
7. Test: thêm test cho `DbUpdateExceptionClassifier` (giữ test cũ nếu có, chỉ đổi tham chiếu); test cho `AiModelCatalogReader` (EF InMemory: tìm thấy / không thấy / `EffectiveModelCode`). Test hiện có chỉ được sửa `using`/tham chiếu kiểu do di chuyển.

## Phạm vi

Được sửa: file nêu ở trên, `using` ở mọi file bị ảnh hưởng bởi D1/D2/D3, `DependencyInjection.cs` (Application), ADR-0006, skill modular-monolith, `ModuleMap.cs`, allowlist (chỉ bớt), test mới trong `tests/EnglishLiveTutor.Domain.Tests/`. Không sửa: migration, cấu hình EF, `appsettings*`, FE, route/DTO JSON.

## Kiểm chứng (trong EnglishPartner.BE)

```bash
dotnet build EnglishLiveTutor.sln --no-restore
MODULE_ALLOWLIST_WRITE=1 dotnet test EnglishLiveTutor.sln --no-build --filter "FullyQualifiedName~ModuleBoundaryTests.NoNewModuleBoundaryViolations"
dotnet test EnglishLiveTutor.sln --no-build
dotnet ef migrations has-pending-model-changes --project src/EnglishLiveTutor.Infrastructure --startup-project src/EnglishLiveTutor.Api --no-build
```

## Ràng buộc

Không commit/push/merge, không thêm package, không in secret, không đổi hành vi. Mơ hồ → dừng, ghi report.

## Worker report (bắt buộc)

```
### Worker report
- File đã sửa/di chuyển: <danh sách, ghi rõ git mv>
- Route trước → sau (D3): <bảng>
- AiModelInfo gồm: <member>
- File giữ IAppDbContext: <file: lý do> hoặc "không"
- Allowlist: <trước → sau theo loại>
- Lệnh đã chạy và kết quả: <lệnh → kết quả, số test>
- Chưa làm / cần quyết định: <danh sách hoặc "không">
```
