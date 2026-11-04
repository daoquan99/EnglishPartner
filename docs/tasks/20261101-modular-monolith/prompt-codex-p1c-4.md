# Việc giao cho Codex: P1c-4 — chuyển handler Grammar sang DbContext của module

Áp dụng **đúng "Quy tắc chung cho mọi gói P1c"** trong `docs/tasks/20261101-modular-monolith/prompt-codex-p1c-1.md` (đọc lại nếu cần) cho:

- `Grammar` → Grammar (`IGrammarDbContext`)

Chỉ sửa file trong các feature trên, helper test `TestDoubles/ModuleDbContextFactoryAdapter.cs` (nếu cần) và test chỉ để sửa wiring constructor; file dùng `DbSet` của module khác thì giữ `IAppDbContext` và liệt kê trong report. Allowlist chỉ được bớt dòng.

Kiểm chứng (trong EnglishPartner.BE):

```bash
dotnet build EnglishLiveTutor.sln --no-restore
MODULE_ALLOWLIST_WRITE=1 dotnet test EnglishLiveTutor.sln --no-build --filter "FullyQualifiedName~ModuleBoundaryTests.NoNewModuleBoundaryViolations"
dotnet test EnglishLiveTutor.sln --no-build
```

Tiêu chí: build 0 lỗi, test 0 fail, `git diff -M` file nguồn chỉ đổi kiểu/`using`. Kết thúc bằng Worker report theo mẫu của P1c-1.
