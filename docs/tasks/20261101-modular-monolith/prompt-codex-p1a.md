# Việc giao cho Codex: P1a — architecture test ranh giới module

## Mục tiêu

Thêm test quét source để kiểm ranh giới module theo ADR-0006, kèm allowlist các vi phạm hiện có (chỉ được giảm dần). Liên quan: ADR-0006 phase P1.

## Bối cảnh cần đọc

- `AGENTS.md` (gốc, luật 13–15) và `EnglishPartner.BE/AGENTS.md`
- `docs/adr/0006-modular-monolith.md` (bảng module + DAG)
- Skill: `EnglishPartner.BE/.agents/skills/modular-monolith/SKILL.md` (mục "Module map" và "Architecture tests")
- Skill: `EnglishPartner.BE/.agents/skills/backend-testing/SKILL.md`
- Plan: `docs/tasks/20261101-modular-monolith/plan.md`

## Phạm vi

Chỉ được tạo/sửa:
- `EnglishPartner.BE/tests/EnglishLiveTutor.Domain.Tests/Architecture/ModuleMap.cs`
- `EnglishPartner.BE/tests/EnglishLiveTutor.Domain.Tests/Architecture/ModuleBoundaryScanner.cs`
- `EnglishPartner.BE/tests/EnglishLiveTutor.Domain.Tests/Architecture/ModuleBoundaryTests.cs`
- `EnglishPartner.BE/tests/EnglishLiveTutor.Domain.Tests/Architecture/module-boundary-allowlist.txt`

Không được sửa: bất kỳ file nào trong `EnglishPartner.BE/src/`, file `.csproj`, test hiện có, migration, `appsettings*`. Không thêm package.

Contract được phép đổi: không có.

## Yêu cầu

1. **Tìm thư mục gốc:** đi ngược từ `AppContext.BaseDirectory` tới thư mục chứa `EnglishLiveTutor.sln`; `src` = `<gốc>/src`. Quét mọi `*.cs` dưới `src`, bỏ qua thư mục `bin`, `obj`, `Migrations`. Đường dẫn trong kết quả là tương đối từ `src`, dùng `/`.

2. **`ModuleMap`** (static), tên module: `Identity`, `Catalog`, `Speech`, `Lessons`, `Grammar`, `Vocabulary`, `LiveTutor`, `Shared`.
   - `ModuleOfNamespace(string ns)` → module hoặc `null` (không xét). Chỉ xét namespace bắt đầu bằng `EnglishLiveTutor.Domain.` hoặc `EnglishLiveTutor.Application.Features.`; lấy segment ngay sau tiền tố:
     - Domain: `Identity`→Identity; `AiCatalog`,`Prompts`,`Voices`,`Topics`→Catalog; `Pronunciation`→Speech; `Lessons`,`LessonAttempts`→Lessons; `Grammar`→Grammar; `Vocabulary`,`Learning`,`VocabularyGames`→Vocabulary; `Conversations`→LiveTutor; `Common`→Shared.
     - Application.Features: `Identity`→Identity; `AiModels`,`Prompts`,`Voices`,`Topics`→Catalog; `Pronunciation`→Speech; `Lessons`→Lessons; `Grammar`→Grammar; `Vocabulary`,`Learning`,`VocabularyGames`→Vocabulary; `Conversations`→LiveTutor.
     - Segment lạ → ném exception có tên segment (để test bắt khi có folder mới chưa map).
   - `ModuleOfFile(string relPath)` → module của file nguồn theo bảng "Module map" trong skill, hoặc `null` cho file Shared Kernel / không thuộc module. Tối thiểu: `EnglishLiveTutor.Domain/<Area>/…` và `EnglishLiveTutor.Application/Features/<Feature>/…` theo cùng bảng segment ở trên (`Domain/Common` → `null`); các file Infrastructure/Api đúng như bảng trong skill (theo thư mục hoặc tên file cụ thể); còn lại `null`.
   - `AllowedDependencies`: Speech→{Catalog}; Lessons→{Speech, Catalog}; Grammar→{Lessons, Vocabulary}; Vocabulary→{Speech, Catalog}; LiveTutor→{Catalog}; Identity, Catalog→{}.

3. **`ModuleBoundaryScanner.Scan()`** trả danh sách `Violation(Kind, SourcePath, Target)` đã khử trùng lặp, sắp xếp ordinal. Chỉ xét file có `ModuleOfFile != null`. Bỏ qua dòng bắt đầu bằng `//` (sau trim). Các loại:
   - `dag`: file module A tham chiếu namespace của module B (B ≠ A, B ≠ Shared) mà B không thuộc `AllowedDependencies[A]`. `Target` = namespace tham chiếu.
   - `internal`: như trên nhưng B được phép, và namespace **không** chứa segment `Contracts`.
   - `appdb`: file thuộc `EnglishLiveTutor.Application/Features/` có token `IAppDbContext` hoặc `IAppDbContextFactory` (regex word-boundary). `Target` = `IAppDbContext` hoặc `IAppDbContextFactory`.
   - `contracts`: file nằm trong thư mục tên `Contracts` có `using Microsoft.EntityFrameworkCore`, hoặc tham chiếu namespace `EnglishLiveTutor.Domain.…` không thuộc `…Enums`/`…Contracts` của module khác, hoặc chứa `IRequestHandler`/`ICommandHandler`/`IQueryHandler`. `Target` = phần vi phạm.
   - Tham chiếu được nhận dạng từ: `using X;`, `global using X;`, `using static X;`, `using Alias = X;`, và tên đầy đủ trong code khớp regex `EnglishLiveTutor\.(Domain|Application\.Features)\.[A-Za-z_][\w.]*`. Với tên đầy đủ có thể kết thúc bằng tên type — chỉ dùng segment module để phân loại; `Target` ghi đúng chuỗi khớp.
   - Định dạng một dòng: `<kind> <SourcePath> -> <Target>`.

4. **Allowlist** `Architecture/module-boundary-allowlist.txt`: đọc từ `<gốc>/tests/EnglishLiveTutor.Domain.Tests/Architecture/` (không copy ra output, không sửa csproj). Dòng trống và dòng bắt đầu `#` bỏ qua. Dòng đầu file là comment giải thích: chỉ được xóa dòng, không thêm (ADR-0006).
   - Khi biến môi trường `MODULE_ALLOWLIST_WRITE=1`: test `NoNewModuleBoundaryViolations` ghi lại toàn bộ vi phạm hiện tại vào file (giữ comment đầu) và pass. Dùng một lần để sinh file ban đầu.

5. **`ModuleBoundaryTests`** (xUnit, namespace `EnglishLiveTutor.Domain.Tests.Architecture`):
   - `NoNewModuleBoundaryViolations`: vi phạm không có trong allowlist → fail, message liệt kê từng dòng.
   - `AllowlistHasNoStaleEntries`: dòng allowlist không còn xảy ra → fail, message liệt kê (để buộc xóa khi đã sửa).
   - `EveryDomainAndFeatureFolderIsMapped`: mọi thư mục cấp 1 trong `src/EnglishLiveTutor.Domain` (trừ `bin`,`obj`) và `src/EnglishLiveTutor.Application/Features` map được qua `ModuleOfNamespace`.
   - `AllowedDependenciesAreAcyclic`: DFS trên `AllowedDependencies`.
   - Test cho scanner với input cố định (truyền nội dung file giả vào một hàm thuần `ScanFile(relPath, content)`): một case cho mỗi loại `dag`, `internal`, `appdb`, `contracts`, và một case tham chiếu `…Contracts` hợp lệ không bị báo.

6. Chạy với `MODULE_ALLOWLIST_WRITE=1` để sinh allowlist, rồi chạy lại không có biến để chắc mọi test architecture pass.

## Tiêu chí hoàn thành

- [ ] Chỉ 4 file trong `tests/EnglishLiveTutor.Domain.Tests/Architecture/` được tạo.
- [ ] `dotnet test --filter "FullyQualifiedName~Architecture"` pass (không đặt biến môi trường).
- [ ] Toàn bộ test: 6 fail có sẵn (`PronunciationSynthesisAudioCacheTests`), còn lại pass; tổng pass ≥ 535 + số test mới.
- [ ] Allowlist có số dòng vi phạm > 0 (hiện đã biết có phụ thuộc Lessons→Pronunciation nội bộ, Grammar→`Domain.Lessons.Enums`, nhiều `appdb`).

## Lệnh kiểm chứng

```bash
# trong EnglishPartner.BE
dotnet build EnglishLiveTutor.sln
MODULE_ALLOWLIST_WRITE=1 dotnet test EnglishLiveTutor.sln --filter "FullyQualifiedName~ModuleBoundaryTests.NoNewModuleBoundaryViolations"
dotnet test EnglishLiveTutor.sln --filter "FullyQualifiedName~Architecture"
dotnet test EnglishLiveTutor.sln
```

(PowerShell: `$env:MODULE_ALLOWLIST_WRITE='1'; dotnet test ...; Remove-Item Env:MODULE_ALLOWLIST_WRITE`.)

Baseline trước khi sửa: 535 pass / 6 fail / 541. Nếu môi trường không cho chạy lệnh, ghi rõ trong report — không đoán kết quả.

## Ràng buộc

- Không commit, push, merge; không tạo branch.
- Không thêm package; không sửa test hiện có; không sửa `src/`.
- Không in hoặc ghi secret, token, API key, connection string.
- Gặp điều mơ hồ hoặc cần sửa ngoài phạm vi: dừng và ghi vào report.

## Worker report (bắt buộc ở cuối câu trả lời)

```
### Worker report
- File đã sửa: <danh sách>
- Đã làm: <tóm tắt từng yêu cầu>
- Số dòng allowlist theo loại: dag=<n>, internal=<n>, appdb=<n>, contracts=<n>
- Lệnh đã chạy và kết quả: <lệnh → pass/fail, số test>
- Chưa làm / cần quyết định: <danh sách hoặc "không">
```
