---
trigger: always_on
---

# EnglishPartner — Core Rules

> **Scope:** Architecture principles & non-negotiable constraints.
> Patterns & templates → `skills.md` | Module rules → `modules/{module}.md` | Infrastructure → `common/{topic}.md`
> KHÔNG ĐƯỢC TỰ Ý HARCODE.

---

## 1. Architecture

- **Modular Monolith** — 7 modules, each with Domain / Application / Infrastructure / Api / Contracts layers
- **CQRS** via MediatR — Commands mutate state, Queries read state. No mixing.
- **DDD** — AggregateRoot owns all state changes. Direct field mutation is forbidden outside the aggregate.
- **Event-Driven** — Cross-module communication via Integration Events (Outbox/Inbox). No direct project references between modules except `.Contracts`.
- **Clean Architecture** — Dependencies flow inward: Api → Application → Domain. Infrastructure implements Application interfaces.

---

## 2. Module Breakdown

| Module | Namespace prefix | DB Schema |
|---|---|---|
| Identity | `EnglishPartner.Modules.Identity` | `identity` |
| AI | `EnglishPartner.Modules.AI` | `ai` |
| Curriculum | `EnglishPartner.Modules.Curriculum` | `curriculum` |
| Learning | `EnglishPartner.Modules.Learning` | `learning` |
| Notifications | `EnglishPartner.Modules.Notifications` | `notification` |
| Gamification | `EnglishPartner.Modules.Gamification` | `gamification` |
| Users | `EnglishPartner.Modules.Users` | `users` |

Cross-module: `EnglishPartner.SharedKernel` (no DB schema — shared abstractions only)

---

## 3. Layer Dependencies

```
Domain          ← SharedKernel               (AggregateRoot, Entity, DomainEvent base types)
Application     ← Domain + SharedKernel.Application + Messaging (CQRS, FluentValidation, IEventBus)
Infrastructure  ← Application + SharedKernel.Application + Contracts (own module)
Api             ← Application + SharedKernel.Extensions
Contracts       ← Messaging                  (IntegrationEvent, IEventBus — for cross-module events)
```

**SharedKernel vs SharedKernel.Application:**
- `SharedKernel` — domain primitives: `AggregateRoot`, `Entity`, `DomainEvent`, `Result<T>`, `Error`
- `SharedKernel.Application` — app-layer abstractions: `ICurrentUserService`, `ICacheService`, `IRepository<T>`, `IUnitOfWork`, `IFileStorageService`, MediatR, FluentValidation, Quartz
- `Messaging` — event bus: `IEventBus`, `IntegrationEvent`, `OutboxMessage`, `InboxMessage`, `IdempotentIntegrationEventHandler`

**Cross-module rule:** Module A may only reference Module B via `B.Contracts`. Never reference B.Application or B.Infrastructure.

---

## 4. CQRS Interfaces

```csharp
// Commands
ICommand                        // returns void (Result)
ICommand<TResponse>             // returns Result<TResponse>
ICommandHandler<TCommand>
ICommandHandler<TCommand, TResponse>

// Queries
IQuery<TResponse>
IQueryHandler<TQuery, TResponse>

// Validation
IValidator<TCommand>            // FluentValidation — registered automatically via MediatR pipeline
```

All handlers are `internal sealed class`. Commands/Queries are `public sealed record`.

---

## 5. Non-Negotiable Rules

1. **NEVER call `SaveChangesAsync()` in a handler** — handled by `UoWBehavior` in the MediatR pipeline.
2. **NEVER use `DateTime.UtcNow` or `DateTimeOffset.UtcNow`** — always inject and use `TimeProvider`.
3. **NEVER hard-delete an AggregateRoot** — use soft-delete with `DeletedBy` + `DeletedAt` audit fields.
4. **NEVER add a FK between two modules** — cross-module data = integration events or Contracts queries.
5. **NEVER inject `HttpContext` or `IHttpContextAccessor` into an Application handler** — use `ICurrentUserService` instead.
6. **NEVER return domain entities from handlers** — always map to DTOs / response records.
7. **NEVER share DbContext between modules** — each module has its own `{Module}DbContext`.
8. **UserId in handlers** — inject `ICurrentUserService`, do NOT pass via Command/Query record.
9. **Authorization** — apply `.RequireAuthorization(policy)` at **group level** in Minimal API, not per-endpoint. **Exception:** Auth endpoints (`/api/v1/auth/*`) use per-endpoint auth vì mix Anonymous + Authenticated routes.
10. **Rate limiting** — all AI-driven endpoints MUST have `.RequireRateLimiting(policy)`.

---

## 6. Naming Conventions

| Artifact | Pattern | Example |
|---|---|---|
| Command | `{Action}{Entity}Command` | `CreateVocabularyCommand` |
| Query | `Get{Entity/Topic}Query` | `GetMySessionsQuery` |
| Handler | `{CommandOrQuery}Handler` | `CreateVocabularyCommandHandler` |
| Validator | `{Command}Validator` | `CreateVocabularyCommandValidator` |
| Domain Event | `{Entity}{Action}DomainEvent` | `VocabularyDeletedDomainEvent` |
| Integration Event | `{Entity}{Action}IntegrationEvent` | `UserRegisteredIntegrationEvent` |
| Error class | `{Module}Errors` | `AiErrors`, `CurriculumErrors` |

---

## 7. API Route Conventions

```
User routes:    api/v1/{module}/{resource}          (RequireAuthorization)
Admin routes:   api/v1/admin/{module}/{resource}    (RequireAuthorization("{permission}"))
```

Versioning via URL prefix `v1`. Group-level auth — never per-endpoint double-auth.