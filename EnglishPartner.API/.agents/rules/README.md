# EnglishPartner — Agent Rules Index

> ⚠️ **MANDATORY AGENT RULE — ĐỌC TRƯỚC KHI LÀM BẤT CỨ ĐIỀU GÌ:**
>
> Sau mỗi lần thay đổi code ảnh hưởng đến một pattern đã được document trong rules:
> 1. **UPDATE file rules liên quan ngay lập tức** — ghi đè nội dung mới trực tiếp vào file
> 2. **KHÔNG show diff** kiểu `old: ... / new: ...` trong chat
> 3. Rules files luôn phản ánh **trạng thái hiện tại** của codebase — không phải lịch sử thay đổi
>
> Ví dụ: sửa ItemType mapping → update `modules/ai.md` ngay. Thêm endpoint → update `common/api.md` ngay.

Chọn file để @mention theo từng task.

## Cấu trúc

```
.agents/rules/
├── rules.md              ← Architecture + Core principles + Non-negotiable rules (LUÔN mention)
├── skills.md             ← Shared services usage + CQRS/DDD code templates (mention khi tạo mới)
│
├── modules/              ← Module-specific rules & patterns
│   ├── identity.md       ← Index → identity/
│   │   └── identity/
│   │       ├── identity_auth.md
│   │       ├── identity_profile.md
│   │       ├── identity_streak.md
│   │       ├── identity_admin_user.md
│   │       └── identity_admin_role.md
│   ├── curriculum.md     ← Index → curriculum/
│   │   └── curriculum/
│   │       ├── curriculum_vocabulary.md
│   │       ├── curriculum_grammar.md
│   │       ├── curriculum_topic.md
│   │       ├── curriculum_exercise.md
│   │       ├── curriculum_conversation.md
│   │       ├── curriculum_example_sentence.md
│   │       └── curriculum_ai_generate.md
│   ├── ai.md             ← Index → ai/
│   │   └── ai/
│   │       ├── ai_core.md
│   │       ├── ai_chat.md
│   │       ├── ai_evaluation.md
│   │       ├── ai_speech.md
│   │       ├── ai_live_avatar.md
│   │       └── ai_admin.md
│   ├── learning.md       ← Index → learning/
│   │   └── learning/
│   │       ├── learning_core.md
│   │       ├── learning_study_session.md
│   │       ├── learning_dashboard.md
│   │       └── learning_admin.md
│   ├── notification.md   ← Index → notification/
│   │   └── notification/
│   │       ├── notification_core.md
│   │       ├── notification_hub.md
│   │       ├── notification_user.md
│   │       └── notification_admin.md
│
└── common/               ← Cross-cutting patterns
    ├── cross_module.md   ← ⭐ Cross-module event map, Module Interfaces, Dependency graph
    ├── database.md       ← DbContext, Migrations, Soft-delete, Repository, Interceptors
    ├── events.md         ← Domain Events, Integration Events, Outbox/Inbox patterns
    ├── caching.md        ← Redis, ICacheService, Key conventions, TTL, ICacheableQuery
    ├── api.md            ← Endpoints, Routes, ProblemDetails, Rate Limiting, SignalR
    ├── domain_driven_design.md  ← AggregateRoot, Entity, ValueObject, DomainEvent
    ├── validation_and_results.md ← FluentValidation, Result<T>, Error
    └── pagination.md     ← PagedResult<T>, PaginationParams
```

---

## Chọn file theo task

| Task | Files cần @mention |
|---|---|
| Làm feature bất kỳ | `rules.md` + `skills.md` + module tương ứng |
| Feature Identity (Auth/JWT) | `rules.md` + `skills.md` + `modules/identity.md` |
| Feature AI (Chat/TTS/Eval) | `rules.md` + `skills.md` + `modules/ai.md` |
| Feature Curriculum (CRUD) | `rules.md` + `skills.md` + `modules/curriculum.md` |
| Feature Learning (SRS) | `rules.md` + `skills.md` + `modules/learning.md` |
| Feature Notification | `rules.md` + `skills.md` + `modules/notification.md` |
| Tạo DbContext / Migration | `rules.md` + `common/database.md` |
| Thêm Integration Event | `rules.md` + `common/events.md` + `common/cross_module.md` |
| Cross-module feature | `rules.md` + `common/cross_module.md` + modules liên quan |
| Thêm Redis Cache | `rules.md` + `common/caching.md` |
| Tạo API endpoint | `rules.md` + `common/api.md` |
| Feature phức tạp | `rules.md` + `skills.md` + module + common liên quan |

---

## Shared Services (từ SharedKernel)

Xem hướng dẫn chi tiết + ví dụ trong `skills.md` → **Skill 1**:

| Service | Khi nào dùng |
|---|---|
| `ICurrentUserService` | Lấy UserId/Email/IsAdmin trong **handler** (KHÔNG ở endpoint) |
| `ICacheService` | Cache-aside cho query read-heavy (`GetOrSetAsync`) |
| `IRepository<T>` | Base repository — Add/Update/Remove/GetById |
| `IUnitOfWork` | KHÔNG inject trong handler — UoWBehavior tự gọi |
| `IFileStorageService` | Upload/download file (avatar, audio, document) |
| `TimeProvider` | Thay thế `DateTime.UtcNow` ở MỌI nơi |

---

## Ví dụ câu lệnh chuẩn

```
@[.agents\rules\rules.md]
@[.agents\rules\skills.md]
@[.agents\rules\modules\learning.md]
@[.agents\rules\common\events.md]

Task: Implement SessionCompletedIntegrationEvent để gửi XP cho Gamification module.
```
