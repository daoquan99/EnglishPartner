# Việc giao cho Codex: P1c-1 — chuyển handler Catalog + Identity sang DbContext của module

## Mục tiêu

Trong các feature dưới đây, thay `IAppDbContext` bằng `I<Module>DbContext` và `IAppDbContextFactory` bằng `IModuleDbContextFactory<I<Module>DbContext>` (đã tạo ở P1b), rồi thu nhỏ allowlist. Chỉ đổi kiểu phụ thuộc, không đổi logic. Liên quan: ADR-0006 P1, luật 15 (chỉ di chuyển).

## Quy tắc chung cho mọi gói P1c (các gói sau sẽ chỉ nêu danh sách feature)

1. Với mỗi file `.cs` trong thư mục feature được giao (`src/EnglishLiveTutor.Application/Features/<Feature>/`) có dòng `appdb` trong `tests/EnglishLiveTutor.Domain.Tests/Architecture/module-boundary-allowlist.txt`:
   - Đổi field/tham số constructor/biến kiểu `IAppDbContext` → `I<Module>DbContext`; `IAppDbContextFactory` → `IModuleDbContextFactory<I<Module>DbContext>`. Thêm `using EnglishLiveTutor.Application.Modules.<Module>;` (và `using EnglishLiveTutor.Application.Modules;` khi dùng factory). Bỏ `using` không còn dùng.
   - Giữ nguyên tên field/biến, thứ tự tham số, mọi câu lệnh LINQ và logic.
   - **Nếu file dùng `DbSet` không thuộc module của nó** (ví dụ handler Lessons đọc `ScoringRubrics` của Speech): **không chuyển file đó**, giữ `IAppDbContext`, ghi vào report dạng `<file>: dùng <DbSet> của <Module khác>`. Đây là việc của P2.
   - Lớp không phải handler trong feature (service, writer, helper) cũng áp dụng như trên.
2. Ngoài `Features/`: chỉ sửa nếu compiler bắt buộc vì đổi kiểu (ví dụ chỗ `new Handler(...)` trong `src/`); ghi rõ trong report.
3. **Test**: chỉ được sửa test khi build test vỡ do đổi kiểu tham số constructor. Cách sửa duy nhất được phép: truyền đúng kiểu mới. `AppDbContext` đã implement mọi interface module nên truyền thẳng instance. Với factory giả hiện có (implement `IAppDbContextFactory`), thêm helper dùng chung `tests/EnglishLiveTutor.Domain.Tests/TestDoubles/ModuleDbContextFactoryAdapter.cs`: `sealed class ModuleDbContextFactoryAdapter<TDb>(IAppDbContextFactory inner) : IModuleDbContextFactory<TDb> where TDb : IModuleDbContext` trả `(TDb)await inner.CreateDbContextAsync(ct)` (tạo nếu chưa có). Không đổi assertion, dữ liệu seed hay tên test.
4. Thu nhỏ allowlist: chạy test với `MODULE_ALLOWLIST_WRITE=1` (chế độ này chỉ xóa dòng stale), rồi chạy lại không có biến. `git diff` của allowlist chỉ được có dòng bị xóa.
5. Không sửa: migration, `Configurations/`, repository, `IAppDbContext.cs`, interface module, DI, `appsettings*`, route/DTO/hub.

## Gói P1c-1

| Feature (`Features/…`) | Module | Interface |
| --- | --- | --- |
| `AiModels`, `Prompts` | Catalog | `ICatalogDbContext` |
| `Identity` | Identity | `IIdentityDbContext` |

(Theo allowlist: AiModels 1, Prompts 3 dùng factory; Identity 13 `IAppDbContext` + 4 factory.)

## Tiêu chí hoàn thành

- [ ] Build 0 lỗi; test 0 fail (baseline sau P1b: tổng pass ≥ số trước gói).
- [ ] Allowlist chỉ bớt dòng; các dòng `appdb` của AiModels/Prompts/Identity biến mất trừ file bị giữ lại có lý do.
- [ ] `git diff -M` trên file nguồn chỉ đổi kiểu/`using`.

## Lệnh kiểm chứng

```bash
# trong EnglishPartner.BE
dotnet build EnglishLiveTutor.sln --no-restore
MODULE_ALLOWLIST_WRITE=1 dotnet test EnglishLiveTutor.sln --no-build --filter "FullyQualifiedName~ModuleBoundaryTests.NoNewModuleBoundaryViolations"
dotnet test EnglishLiveTutor.sln --no-build
```

## Ràng buộc

Không commit/push/merge, không thêm package, không in secret. Mơ hồ → dừng, ghi report.

## Worker report (bắt buộc)

```
### Worker report
- File đã sửa: <danh sách>
- File giữ IAppDbContext (dùng DbSet module khác): <file: DbSet → module> hoặc "không"
- Test đã sửa (chỉ wiring): <danh sách> hoặc "không"
- Allowlist: <số dòng trước → sau>
- Lệnh đã chạy và kết quả: <lệnh → pass/fail, số test>
- Chưa làm / cần quyết định: <danh sách hoặc "không">
```
