# EnglishPartner — Full-Stack Agent Rules Index

> ⚠️ **MANDATORY AGENT RULE — ĐỌC TRƯỚC KHI LÀM BẤT CỨ ĐIỀU GÌ:**
>
> Sau mỗi lần thay đổi code ảnh hưởng đến một pattern đã được document trong rules:
> 1. **UPDATE file rules liên quan ngay lập tức** — ghi đè nội dung mới trực tiếp vào file
> 2. **KHÔNG show diff** kiểu `old: ... / new: ...` trong chat
> 3. Rules files luôn phản ánh **trạng thái hiện tại** của codebase — không phải lịch sử thay đổi
>
> Ví dụ: sửa ItemType mapping → update `backend/modules/ai.md` ngay. Thêm endpoint → update `backend/common/api.md` ngay.

Chọn file để @mention theo từng task.

---

## Cấu trúc

```
.agents/rules/
├── developer_agent.md           ← Agent persona + core principles (LUÔN mention)
├── README.md                    ← This file — index & navigation guide
│
├── backend/                     ← Backend (C# .NET) rules
│   ├── rules.md                 ← Architecture + CQRS + DDD + Non-negotiable rules
│   ├── skills.md                ← Shared services + Code templates
│   ├── modules/                 ← Module-specific rules & patterns
│   │   ├── identity.md          ← Index → identity/
│   │   ├── ai.md                ← Index → ai/
│   │   ├── curriculum.md        ← Index → curriculum/
│   │   ├── learning.md          ← Index → learning/
│   │   └── notification.md      ← Index → notification/
│   └── common/                  ← Cross-cutting patterns
│       ├── cross_module.md      ← Cross-module event map, Module Interfaces
│       ├── database.md          ← DbContext, Migrations, Soft-delete, Repository
│       ├── events.md            ← Domain Events, Integration Events, Outbox/Inbox
│       ├── caching.md           ← Redis, ICacheService, Key conventions
│       ├── api.md               ← Endpoints, Routes, ProblemDetails, Rate Limiting, SignalR
│       ├── domain_driven_design.md  ← AggregateRoot, Entity, ValueObject
│       ├── validation_and_results.md ← FluentValidation, Result<T>, Error
│       └── pagination.md        ← PagedResult<T>, PaginationParams
│
├── frontend/                    ← Frontend (Next.js) rules
│   ├── rules.md                 ← Stack, Component rules, Anti-patterns, Naming
│   ├── skills.md                ← Feature module, API integration, Forms, Zustand
│   └── structure.md             ← Project file tree (auto-maintained)
│
└── fullstack.md                 ← Full-stack integration rules (API contracts, Auth flow, SignalR)
```

---

## Chọn file theo task

### Full-Stack Tasks

| Task | Files cần @mention |
|---|---|
| Full-stack feature (BE + FE) | `developer_agent.md` + `fullstack.md` + BE & FE rules + module |
| API contract change | `fullstack.md` + `backend/common/api.md` + `frontend/rules.md` |
| Auth/Security change | `fullstack.md` + `backend/modules/identity.md` + `frontend/rules.md` |
| SignalR/Realtime feature | `fullstack.md` + `backend/common/api.md` + `frontend/skills.md` |
| Debug cross-stack issue | `fullstack.md` + relevant BE & FE modules |

### Backend-Only Tasks

| Task | Files cần @mention |
|---|---|
| Feature bất kỳ | `backend/rules.md` + `backend/skills.md` + module tương ứng |
| Feature Identity (Auth/JWT) | `backend/rules.md` + `backend/skills.md` + `backend/modules/identity.md` |
| Feature AI (Chat/TTS/Eval) | `backend/rules.md` + `backend/skills.md` + `backend/modules/ai.md` |
| Feature Curriculum (CRUD) | `backend/rules.md` + `backend/skills.md` + `backend/modules/curriculum.md` |
| Feature Learning (SRS) | `backend/rules.md` + `backend/skills.md` + `backend/modules/learning.md` |
| Tạo DbContext / Migration | `backend/rules.md` + `backend/common/database.md` |
| Thêm Integration Event | `backend/rules.md` + `backend/common/events.md` + `backend/common/cross_module.md` |

### Frontend-Only Tasks

| Task | Files cần @mention |
|---|---|
| Feature bất kỳ | `frontend/rules.md` + `frontend/skills.md` |
| Thêm page mới | `frontend/rules.md` + `frontend/skills.md` + `frontend/structure.md` |
| Thêm component mới | `frontend/rules.md` + `frontend/structure.md` |
| Thêm API integration | `frontend/rules.md` + `frontend/skills.md` |
| Fix styling/UI | `frontend/rules.md` |

---

## Quick Reference — Shared Services

### Backend (from SharedKernel)

| Service | Khi nào dùng |
|---|---|
| `ICurrentUserService` | Lấy UserId/Email/IsAdmin trong **handler** (KHÔNG ở endpoint) |
| `ICacheService` | Cache-aside cho query read-heavy (`GetOrSetAsync`) |
| `IRepository<T>` | Base repository — Add/Update/Remove/GetById |
| `IUnitOfWork` | KHÔNG inject trong handler — UoWBehavior tự gọi |
| `IFileStorageService` | Upload/download file (avatar, audio, document) |
| `TimeProvider` | Thay thế `DateTime.UtcNow` ở MỌI nơi |

### Frontend (from lib/)

| Service | Khi nào dùng |
|---|---|
| `apiClient` (`lib/axios`) | Mọi HTTP call — KHÔNG import axios trực tiếp |
| `useAuthStore` (`lib/zustand`) | Access token + user state — in-memory only |
| React Query hooks | Data fetching — KHÔNG gọi API trực tiếp trong component |
| `cn()` (`shared/utils`) | Conditional Tailwind classes |
