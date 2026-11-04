# P3 — tách project theo module (thiết kế của orchestrator)

Mục tiêu: ranh giới module được **compiler** bảo đảm. Không đổi namespace (model snapshot EF chứa tên CLR đầy đủ của entity), route, JSON, hub, DB. Build + toàn bộ test giữ nguyên.

## Cấu trúc đích

```text
src/
  EnglishLiveTutor.Domain/                    chỉ còn Domain/Common (domain kernel, BCL only)
  EnglishLiveTutor.SharedKernel/              (mới) Application/Common/** (trừ file module-specific), Application/Modules/IModuleDbContext*.cs
  Modules/<X>/EnglishLiveTutor.Modules.<X>.Contracts/   Features/<F>/Contracts/** của module X (+ DTO mà Contracts cần)
  Modules/<X>/EnglishLiveTutor.Modules.<X>/             Domain/<Area>/**, Features/<F>/** (trừ Contracts), Application/Modules/<X>/I<X>DbContext.cs, file module-specific từ Common, DI extension Add<X>Module()
  EnglishLiveTutor.Application/               composition: DependencyInjection (gọi Add<X>Module, MediatR + FluentValidation quét mọi assembly module), pipeline behaviors nếu chưa chuyển vào SharedKernel
  EnglishLiveTutor.Infrastructure/            giữ nguyên vị trí (AppDbContext, configurations, repositories, adapters); tham chiếu mọi module
  EnglishLiveTutor.Api/                       host; endpoints/hubs giữ nguyên chỗ
```

Thư mục bên trong project module giữ cấu trúc cũ để namespace không đổi, ví dụ `Modules/Lessons/EnglishLiveTutor.Modules.Lessons/Domain/Lessons/LessonEntity.cs` (namespace `EnglishLiveTutor.Domain.Lessons`), `…/Features/Lessons/Commands/…` (namespace `EnglishLiveTutor.Application.Features.Lessons…`). Đặt `<RootNamespace>` phù hợp; tắt cảnh báo namespace/thư mục nếu có analyzer.

Tham chiếu project:

| Project | ProjectReference |
| --- | --- |
| SharedKernel | Domain |
| Modules.X.Contracts | SharedKernel (+ Domain); Contracts của module trong DAG nếu chữ ký cần (ví dụ Lessons.Contracts → Speech.Contracts) |
| Modules.X | SharedKernel, Modules.X.Contracts, Contracts của module trong DAG (Identity.Contracts cho mọi module) |
| Application | SharedKernel, mọi Modules.X (+ Contracts) |
| Infrastructure | Application (giữ), mọi Modules.X |
| Api, tests | như cũ (transitive) + thêm nếu compiler cần |

Package: Modules.X và SharedKernel dùng các package Application đang dùng (MediatR, FluentValidation, EF Core) — cùng version, không thêm package mới.

## Quy tắc

- `IAppDbContext`/`IAppDbContextFactory` (gộp 7 interface module) chuyển sang project Infrastructure, **giữ namespace** `EnglishLiveTutor.Application.Common.Interfaces` (chỉ Infrastructure và tests còn dùng).
- File trong `Application/Common` tham chiếu type của module → **không** vào SharedKernel; chuyển vào module sở hữu khi tách module đó (P3a để lại trong Application, ghi danh sách).
- MediatR/FluentValidation/DI: đăng ký phải bao phủ mọi assembly module. `CqrsDependencyInjectionTests` phải quét **mọi** assembly chứa request (không giảm số request được kiểm — ghi số trước/sau).
- Architecture test: `ModuleMap` map đường dẫn `src/Modules/<X>/` → module X, `src/EnglishLiveTutor.SharedKernel/` → Shared. Allowlist giữ 0. Thêm test: project module chỉ có `ProjectReference` đúng bảng trên (đọc csproj).
- `InternalsVisibleTo` không cần trong P3 (chưa đổi `public` → `internal`; để sau).

## Gói

| Gói | Nội dung |
| --- | --- |
| P3a | Tạo SharedKernel; chuyển `Application/Common` (shared) + `Application/Modules/IModuleDbContext*.cs`; chuyển `IAppDbContext` sang Infrastructure; composition DI quét nhiều assembly (hạ tầng cho các gói sau); ModuleMap path mới; test csproj reference |
| P3b | Catalog |
| P3c | Identity |
| P3d | Speech |
| P3e | Vocabulary |
| P3f | Lessons |
| P3g | Grammar |
| P3h | LiveTutor; dọn `Domain` (chỉ còn Common) và `Application` (chỉ composition) |

Mỗi gói: `dotnet build` 0 lỗi, `dotnet test` cùng số pass (≥ 598) 0 fail, `has-pending-model-changes` không đổi, `git diff -M --stat` cho thấy rename (nội dung file di chuyển chỉ khác ở mức 0 dòng, trừ file DI/csproj/sln).
