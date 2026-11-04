# P1a fix 1 — sửa theo code review

Cùng 4 file trong `EnglishPartner.BE/tests/EnglishLiveTutor.Domain.Tests/Architecture/`. Không sửa file nào khác.

1. **`ModuleBoundaryScanner.cs` (~124-138), loại `contracts`:** báo mọi tham chiếu `EnglishLiveTutor.Domain.*` trong file Contracts nếu namespace không chứa segment `Enums` hoặc `Contracts` — **kể cả của chính module**. Bỏ qua namespace thuộc Shared (`EnglishLiveTutor.Domain.Common…`). Thêm test: Contracts dùng `EnglishLiveTutor.Domain.Lessons.Entities` từ file Lessons → báo; dùng `EnglishLiveTutor.Domain.Common.Primitives` → không báo; dùng `EnglishLiveTutor.Domain.Lessons.Enums` → không báo.
2. **`ModuleBoundaryTests.cs` (~15-22), chế độ ghi:** khi file allowlist đã tồn tại, `MODULE_ALLOWLIST_WRITE=1` chỉ được ghi giao của allowlist hiện có và vi phạm hiện tại (tức chỉ xóa dòng stale), và test vẫn **fail** nếu có vi phạm mới không nằm trong allowlist. Chỉ khi file chưa tồn tại mới ghi toàn bộ.
3. **Thêm test scanner** (dùng `ScanFile`, có thể `[Theory]`): `global using`, `using static`, `using Alias = …`, tên đầy đủ trong code, dòng `//` bị bỏ qua, `contracts` với `IRequestHandler`/`ICommandHandler`/`IQueryHandler`; và `ModuleOfFile`: `EnglishLiveTutor.Infrastructure/Ai/CefrPromptBuilder.cs` → Catalog, `EnglishLiveTutor.Infrastructure/Persistence/AppDbContext.cs` → null, `EnglishLiveTutor.Api/Hubs/TutorConversationHub.cs` → LiveTutor.
4. Test ~198-200: thay `DoesNotContain(... EndsWith("/Handler.cs"))` bằng `Assert.Empty(violations)`.
5. **Test mới `EveryInfrastructureAndApiFileIsClassified`:** mọi `.cs` dưới `src/EnglishLiveTutor.Infrastructure` và `src/EnglishLiveTutor.Api` (trừ `bin`, `obj`, `Migrations`, `Properties`) hoặc có `ModuleOfFile != null`, hoặc nằm trong danh sách Shared Kernel tường minh (`ModuleMap.SharedKernelFiles` — thêm mới, liệt kê đúng các file/thư mục Shared Kernel theo bảng trong skill). File không thuộc cả hai → fail với danh sách.
6. Test kiểm dòng đầu của `module-boundary-allowlist.txt` là comment bắt đầu `#`.

Allowlist hiện có không được thêm dòng. Chạy lại:

```bash
dotnet build EnglishLiveTutor.sln --no-restore
dotnet test EnglishLiveTutor.sln --no-build --filter "FullyQualifiedName~Architecture"
dotnet test EnglishLiveTutor.sln --no-build
```

Baseline: 544 pass / 6 fail có sẵn (`PronunciationSynthesisAudioCacheTests`). Kết thúc bằng Worker report (file đã sửa, đã làm, số test, allowlist theo loại).
