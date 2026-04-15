# EnglishPartner - Project Rules

## 1. Architecture Overview

**EnglishPartner** is a .NET 10 backend API for a language learning application (initially English, designed for multi-language expansion), built using **Modular Monolith** architecture combined with **CQRS**, **Domain-Driven Design (DDD)**, and **Event-Driven Design**.

### Core Principles
- **Modular Monolith**: Single deployable unit, logically separated by bounded contexts (modules).
- **CQRS**: Strict separation of Commands (writes) and Queries (reads) within each module.
- **DDD**: Rich domain models with Aggregates, Entities, Value Objects, and Domain Events.
- **Event-Driven**: Modules communicate through Integration Events via an in-process message bus.
- **User vs Admin Separation**: All API endpoints are separated into User-facing and Admin-facing routes.
- **Multi-Language Ready**: Architecture supports future expansion to other languages (Chinese, Japanese, Korean, etc.) via `Language` entity and `LanguageCode` properties.

---

## 2. Module Breakdown

The application consists of **7 modules**, each representing a bounded context:

| # | Module | Namespace Prefix | DB Schema | Description |
|---|---|---|---|---|
| 0 | **SharedKernel** | `EnglishPartner.SharedKernel` | — | Base classes, Result, Errors, Abstractions |
| 0 | **Messaging** | `EnglishPartner.Messaging` | — | Event bus, Integration events |
| 1 | **Identity** | `EnglishPartner.Modules.Identity` | `identity` | Auth, Users, Roles, Permissions, Onboarding, Streaks |
| 2 | **AI** | `EnglishPartner.Modules.AI` | `ai` | Gemini API integration, Chatbot, Scoring |
| 3 | **Curriculum** | `EnglishPartner.Modules.Curriculum` | `curriculum` | Vocabulary, Grammar, Tenses, Topics, Exercises, Conversations |
| 4 | **Learning** | `EnglishPartner.Modules.Learning` | `learning` | User progress, Practice logs, Scores, SRS (SM-2) |
| 5 | **Gamification** | `EnglishPartner.Modules.Gamification` | `gamification` | XP, Achievements, Leaderboard, Daily Goals |
| 6 | **Notification** | `EnglishPartner.Modules.Notification` | `notification` | In-app notifications, Reminders |

---

## 3. Solution Structure

```
EnglishPartner.sln
├── src/
│   ├── Apps/
│   │   └── EnglishPartner.Api/                        # Host / Composition Root
│   ├── Modules/
│   │   ├── Identity/
│   │   │   ├── EnglishPartner.Modules.Identity.Api/
│   │   │   ├── EnglishPartner.Modules.Identity.Application/
│   │   │   ├── EnglishPartner.Modules.Identity.Domain/
│   │   │   ├── EnglishPartner.Modules.Identity.Infrastructure/
│   │   │   └── EnglishPartner.Modules.Identity.Contracts/
│   │   ├── AI/
│   │   │   ├── EnglishPartner.Modules.AI.Api/
│   │   │   ├── EnglishPartner.Modules.AI.Application/
│   │   │   ├── EnglishPartner.Modules.AI.Domain/
│   │   │   ├── EnglishPartner.Modules.AI.Infrastructure/
│   │   │   └── EnglishPartner.Modules.AI.Contracts/
│   │   ├── Curriculum/
│   │   │   ├── EnglishPartner.Modules.Curriculum.Api/
│   │   │   ├── EnglishPartner.Modules.Curriculum.Application/
│   │   │   ├── EnglishPartner.Modules.Curriculum.Domain/
│   │   │   ├── EnglishPartner.Modules.Curriculum.Infrastructure/
│   │   │   └── EnglishPartner.Modules.Curriculum.Contracts/
│   │   ├── Learning/
│   │   │   ├── EnglishPartner.Modules.Learning.Api/
│   │   │   ├── EnglishPartner.Modules.Learning.Application/
│   │   │   ├── EnglishPartner.Modules.Learning.Domain/
│   │   │   ├── EnglishPartner.Modules.Learning.Infrastructure/
│   │   │   └── EnglishPartner.Modules.Learning.Contracts/
│   │   ├── Gamification/
│   │   │   ├── EnglishPartner.Modules.Gamification.Api/
│   │   │   ├── EnglishPartner.Modules.Gamification.Application/
│   │   │   ├── EnglishPartner.Modules.Gamification.Domain/
│   │   │   ├── EnglishPartner.Modules.Gamification.Infrastructure/
│   │   │   └── EnglishPartner.Modules.Gamification.Contracts/
│   │   └── Notification/
│   │       ├── EnglishPartner.Modules.Notification.Api/
│   │       ├── EnglishPartner.Modules.Notification.Application/
│   │       ├── EnglishPartner.Modules.Notification.Domain/
│   │       ├── EnglishPartner.Modules.Notification.Infrastructure/
│   │       └── EnglishPartner.Modules.Notification.Contracts/
│   └── Shared/
│       ├── EnglishPartner.SharedKernel/                # PURE domain primitives: Entity, AggregateRoot, Result, Errors (NO frameworks)
│       ├── EnglishPartner.SharedKernel.Application/     # CQRS, Behaviors, Interceptors, Settings (MediatR, EF Core, FluentValidation)
│       └── EnglishPartner.Messaging/                   # Event bus abstractions
└── tests/
    └── Modules/
        ├── Identity/
        │   ├── EnglishPartner.Modules.Identity.UnitTests/
        │   └── EnglishPartner.Modules.Identity.IntegrationTests/
        └── ... (same pattern for each module)
```

### 3.1 Domain Project Internal Structure (Per Module)

Each module's Domain project is organized **by Aggregate Root**:

```
{Module}.Domain/
├── {AggregateRoot}/              ← Folder per aggregate
│   ├── {AggregateRoot}.cs        ← AggregateRoot entity at folder root
│   ├── Entities/                 ← Child entities of this aggregate
│   │   ├── ChildEntityA.cs
│   │   └── ChildEntityB.cs
│   ├── Repositories/             ← Repository interfaces for this aggregate
│   │   └── I{AggregateRoot}Repository.cs
│   └── Events/                   ← Domain events for this aggregate
│       └── {AggregateRoot}DomainEvents.cs
└── {Module}Errors.cs             ← All domain errors for this module
```

**Rules:**
- **AggregateRoot** sits at the **root** of its folder — it is the entry point
- **Child entities** (Entity) go inside `Entities/` subfolder
- **Repository interfaces** go inside `Repositories/` subfolder
- **Domain events** go inside `Events/` subfolder
- **Errors** are at the module root level (e.g., `IdentityErrors.cs`)

### 3.2 Application Layer Abstractions

Infrastructure concerns are defined as interfaces in `Application/Abstractions/`, NOT in Domain:

```
{Module}.Application/
├── Abstractions/                  ← Infrastructure service interfaces
│   ├── IPasswordHasher.cs
│   ├── ITokenService.cs
│   └── ...
├── Auth/                          ← Feature folders (CQRS)
│   └── Commands/
│       └── Login/
│           ├── LoginCommand.cs
│           ├── LoginCommandHandler.cs
│           └── LoginCommandValidator.cs
└── DependencyInjection.cs
```

**Rule:** Anything that is "how" (hashing, token generation, email sending) belongs in `Application/Abstractions/`. Domain only contains "what" (business rules, invariants).

### 3.3 CQRS Marker Interfaces & Handler Abstractions

All commands and queries MUST use marker interfaces from `SharedKernel.Application.CQRS`:

```csharp
// Commands and Queries (in SharedKernel.Application)
public interface ICommand : IRequest<Result>;
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;

// Handler interfaces (in SharedKernel.Application) — use THESE, not IRequestHandler
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand;
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
```

**Rule:** Handlers MUST implement `ICommandHandler` or `IQueryHandler`, NOT `IRequestHandler` directly.

```csharp
// ✅ Correct
internal sealed class RegisterCommandHandler : ICommandHandler<RegisterCommand, RegisterResponse>
// ❌ Wrong
internal sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
```

### 3.4 Behaviors Pipeline

**MediatR Pipeline Order:** `LoggingBehavior` → `ValidationBehavior` → `{Module}UnitOfWorkBehavior` → Handler

| Behavior | Location | Scope | Purpose |
|----------|----------|-------|---------|
| `LoggingBehavior` | SharedKernel.Application | Global | Logs name, duration, warns on slow (>500ms) |
| `ValidationBehavior` | SharedKernel.Application | Global | Auto-validates via FluentValidation |
| `{Module}UnitOfWorkBehavior` | {Module}.Application | Per-module | Auto-calls SaveChangesAsync on THIS module's DbContext |

**UnitOfWorkBehavior is PER-MODULE, not global.** Each module defines its own UoW behavior that:
1. Checks if the request belongs to the module's assembly (`typeof(TRequest).Assembly`)
2. Depends on `I{Module}UnitOfWork` interface (defined in Application, implemented by DbContext in Infrastructure)
3. Only fires for `ICommand` types, skips `IQuery`

```csharp
// Application/Abstractions/IIdentityUnitOfWork.cs — interface in Application
public interface IIdentityUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

// Application/Behaviors/IdentityUnitOfWorkBehavior.cs — behavior in Application
internal sealed class IdentityUnitOfWorkBehavior<TRequest, TResponse>(
    IIdentityUnitOfWork unitOfWork) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly Assembly ModuleAssembly = typeof(IdentityUnitOfWorkBehavior<,>).Assembly;
    // Only SaveChanges if request is from THIS module and is a command
}

// Infrastructure/Persistence/IdentityDbContext.cs — implements in Infrastructure
public sealed class IdentityDbContext : DbContext, IIdentityUnitOfWork { }

// Infrastructure/DependencyInjection.cs — wiring
services.AddScoped<IIdentityUnitOfWork>(sp => sp.GetRequiredService<IdentityDbContext>());
```

**Rule:** Handlers MUST NOT call `SaveChangesAsync()` directly.

### 3.5 Clean Architecture Dependency Graph

```
Domain            → SharedKernel                         (pure: Entity, AggregateRoot, Result — NO frameworks)
Application       → Domain + SharedKernel.Application    (CQRS, Behaviors, Validation)
Infrastructure    → Application + SharedKernel.Application    (EF Core, Interceptors, Repos)
Api               → Application + Infrastructure         (Endpoints, Module installer)
```

**CRITICAL: Domain MUST NOT reference SharedKernel.Application, MediatR, EF Core, FluentValidation, or ASP.NET Core.**

---


## 4. Base Class Hierarchy (SharedKernel)

### 4.1 Entity (Base for ALL entities)

Every entity in the system inherits from `Entity` which includes **audit tracking**:

```csharp
public abstract class Entity : IEquatable<Entity>
{
    public Guid Id { get; protected set; }

    // ── Audit Fields ──
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }        // UserId who created
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }        // UserId who last updated
}
```

**Rules:**
- `CreatedAt` and `CreatedBy` are set **once** at creation time and **never** modified.
- `UpdatedAt` and `UpdatedBy` are updated on **every modification**.
- Audit fields are populated **automatically** via EF Core `SaveChangesInterceptor` — handlers/services do NOT set these manually.
- `CreatedBy`/`UpdatedBy` are `Guid?` (nullable) to support system-generated entities (seed data, AI-generated content) where no user context exists.

### 4.2 AggregateRoot (Base for root entities only)

Aggregate roots extend Entity with **soft-delete**, **active status**, and **domain events**:

```csharp
public abstract class AggregateRoot : Entity
{
    // ── Lifecycle Fields ──
    public bool IsActive { get; protected set; } = true;
    public bool IsDeleted { get; protected set; }
    public DateTime? DeletedAt { get; protected set; }
    public Guid? DeletedBy { get; protected set; }  // UserId who deleted

    // ── Domain Events ──
    private readonly List<DomainEvent> _domainEvents = [];
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // ── Behaviors ──
    public virtual void Activate() { IsActive = true; }
    public virtual void Deactivate() { IsActive = false; }
    public virtual void SoftDelete(Guid? deletedBy = null)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        IsActive = false;
    }
    public virtual void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        IsActive = true;
    }

    protected void RaiseDomainEvent(DomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

**Rules:**
- `IsDeleted` = soft-delete flag. Entities with `IsDeleted = true` are **excluded by default** via EF Core global query filters.
- `IsActive` = business-level toggle. Admin can deactivate content without deleting it. Does NOT trigger soft-delete.
- `DeletedAt`/`DeletedBy` are set **only** when `SoftDelete()` is called.
- **Hard-delete is FORBIDDEN** except for truly ephemeral data (refresh tokens, temp logs).
- `SoftDelete()` automatically sets `IsActive = false`. `Restore()` reverses both.

### 4.3 When to use which base class

| Base Class | Use When | Examples |
|---|---|---|
| `Entity` | Child entity within an aggregate, junction table, log record | `ConversationTurn`, `ExerciseOption`, `UserRole`, `ChatMessage`, `LearningLog` |
| `AggregateRoot` | Root entity that owns a consistency boundary | `User`, `Vocabulary`, `Grammar`, `Topic`, `Exercise`, `ChatSession`, `Notification` |

### 4.4 Audit Interceptor (EF Core)

```csharp
// Registered in each module's Infrastructure
public sealed class AuditableEntityInterceptor(ICurrentUserService currentUser)
    : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(...)
    {
        foreach (var entry in context.ChangeTracker.Entries<Entity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.CreatedBy = currentUser.UserId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedBy = currentUser.UserId;
                    break;
            }
        }
        // Handle soft-delete for AggregateRoot entries...
    }
}
```

---

## 5. Layer Responsibilities (Per Module)

### 5.1 Domain Layer (`Module.Domain`)
- **NO dependencies** on other layers or external packages (except SharedKernel).
- Contains: Aggregates, Entities, Value Objects, Domain Events, Repository Interfaces, Domain Services, Enums, Exceptions.
- All classes are `internal` by default.
- Entities use **private setters** and expose behavior through methods.
- Domain Events inherit from `DomainEvent` (SharedKernel).

### 5.2 Application Layer (`Module.Application`)
- Depends on: Domain layer only.
- Contains: Commands, Queries, Handlers, Validators (FluentValidation), DTOs, Mappers, Application Services.
- Uses **MediatR** for dispatching Commands/Queries.
- Commands return `Result<T>` or `Result` (from SharedKernel).
- Queries return DTOs, never domain entities.
- Cross-cutting behaviors via MediatR Pipeline Behaviors (Validation, Logging, Transaction).

### 5.3 Infrastructure Layer (`Module.Infrastructure`)
- Depends on: Domain + Application layers.
- Contains: EF Core DbContext, Repository Implementations, External Service Clients, Migrations, Interceptors.
- Each module has its **own DbContext** with its own schema.
- Repositories implement interfaces defined in the Domain layer.
- **Global Query Filters**: Apply `IsDeleted == false` filter on all `AggregateRoot` entities.
- **AuditableEntityInterceptor**: Auto-populate audit fields on SaveChanges.

### 5.4 API Layer (`Module.Api`)
- Depends on: Application layer (only sends Commands/Queries via MediatR).
- Contains: Minimal API endpoint definitions, organized into `Endpoints/User/` and `Endpoints/Admin/`.
- **Thin endpoints** — only HTTP concerns, delegate all logic to handlers.
- Registers module via `IModuleInstaller` pattern.

### 5.5 Contracts Layer (`Module.Contracts`)
- **NO internal dependencies**. Can be referenced by other modules.
- Contains: Integration Events, Public DTOs, Module interfaces.
- This is the ONLY project other modules may reference.

---

## 6. Coding Standards

### 6.1 Naming Conventions
- **Commands**: `{Verb}{Noun}Command` → `CreateVocabularyCommand`
- **Queries**: `Get{Noun}Query` → `GetVocabularyByIdQuery`
- **Handlers**: `{CommandOrQuery}Handler` → `CreateVocabularyCommandHandler`
- **Validators**: `{CommandOrQuery}Validator` → `CreateVocabularyCommandValidator`
- **Domain Events**: `{Noun}{PastTenseVerb}DomainEvent` → `VocabularyCreatedDomainEvent`
- **Integration Events**: `{Noun}{PastTenseVerb}IntegrationEvent` → `UserRegisteredIntegrationEvent`
- **Endpoints**: Group by feature in static classes → `VocabularyEndpoints.cs`
- **DbContext**: `{Module}DbContext` → `CurriculumDbContext`
- **Interceptors**: `{Purpose}Interceptor` → `AuditableEntityInterceptor`

### 6.2 File Organization (within Application layer)
```
Application/
├── {Feature}/
│   ├── Commands/
│   │   ├── Create{Entity}/
│   │   │   ├── Create{Entity}Command.cs
│   │   │   ├── Create{Entity}CommandHandler.cs
│   │   │   └── Create{Entity}CommandValidator.cs
│   │   └── Update{Entity}/
│   │       └── ...
│   ├── Queries/
│   │   ├── Get{Entity}ById/
│   │   │   ├── Get{Entity}ByIdQuery.cs
│   │   │   ├── Get{Entity}ByIdQueryHandler.cs
│   │   │   └── Get{Entity}ByIdResponse.cs (DTO)
│   │   └── GetAll{Entities}/
│   │       └── ...
│   └── EventHandlers/
│       └── {DomainEvent}Handler.cs
├── Contracts/
│   └── DTOs/
└── DependencyInjection.cs
```

### 6.3 API Endpoint Organization
```
Api/
├── Endpoints/
│   ├── User/
│   │   ├── VocabularyEndpoints.cs       # GET, practice-related
│   │   └── ExerciseEndpoints.cs
│   └── Admin/
│       ├── VocabularyEndpoints.cs       # CRUD management
│       └── ExerciseEndpoints.cs
├── Filters/
└── ModuleInstaller.cs
```

### 6.4 API Route Conventions
- **User-facing**: `api/v{version}/{module}/{resource}`
  - Example: `api/v1/curriculum/vocabularies`
- **Admin-facing**: `api/v{version}/admin/{module}/{resource}`
  - Example: `api/v1/admin/curriculum/vocabularies`

---

## 6.5 Comment Policy

**Minimize comments in code.** Code should be self-documenting through clear naming.

**DO comment:**
- **XML doc comment (`///`) at the top of each class** — briefly describe the class purpose.
- **XML doc comment (`///`) on complex/non-obvious methods** — explain *why*, not *what*.
- **Domain Event classes** — describe what triggered this event and what consumers should do.
- **Integration Event classes** — describe the cross-module contract.
- **Complex business logic** — explain the *why* behind non-trivial algorithms (e.g., SM-2, scoring formulas).

**DO NOT comment:**
- Simple CRUD operations, getters/setters, straightforward methods.
- Obvious code like `// Save to database` above `SaveChangesAsync()`.
- Inline comments explaining *what* code does (the code itself should be clear).
- Commented-out code — delete it, Git has history.

**Example:**
```csharp
/// <summary>
/// Manages vocabulary entities within the Curriculum bounded context.
/// </summary>
public sealed class Vocabulary : AggregateRoot
{
    public static Vocabulary Create(string word, string definition, string languageCode)
    {
        // No comment needed — method name and parameters are self-explanatory
        var entity = new Vocabulary { ... };
        entity.RaiseDomainEvent(new VocabularyCreatedDomainEvent(entity.Id));
        return entity;
    }

    /// <summary>
    /// Applies SM-2 spaced repetition interval calculation.
    /// Quality 0-2 resets progress; 3-5 increases interval by ease factor.
    /// </summary>
    public void CalculateNextReview(int quality) { ... }
}

/// <summary>
/// Raised when a new vocabulary is created.
/// Consumed by: Learning module (initialize progress), Notification module (new content alert).
/// </summary>
public sealed record VocabularyCreatedDomainEvent(Guid VocabularyId) : DomainEvent;
```

---

## 7. Technology Stack

| Category | Technology |
|---|---|
| Framework | .NET 10, ASP.NET Core 10 |
| ORM | Entity Framework Core 10 |
| Database | PostgreSQL (with schema-per-module) + `pg_trgm` + `unaccent` |
| CQRS/Mediator | MediatR |
| Validation | FluentValidation |
| Auth | ASP.NET Core Identity + JWT Bearer |
| Caching | Redis (StackExchange.Redis / Microsoft.Extensions.Caching.StackExchangeRedis) |
| AI Integration | Google Gemini 3.1 Flash Lite (`gemini-3.1-flash-lite-preview`) via Google.GenerativeAI SDK |
| Real-time | SignalR (chat streaming, live notifications) |
| File Storage | Local filesystem + `IFileStorageService` abstraction |
| Background Jobs | Hangfire (with PostgreSQL storage) |
| Logging | Serilog + Seq (dev) |
| API Documentation | Scalar (OpenAPI) |
| API Versioning | Asp.Versioning |
| Health Checks | ASP.NET Core HealthChecks (PostgreSQL, Redis, Gemini) |
| Telemetry | OpenTelemetry |
| Rate Limiting | ASP.NET Core Rate Limiting (per-user + global) |
| Testing | xUnit, Testcontainers, NSubstitute |
| DI Scanning | Scrutor |
| Search (Phase 2) | Elasticsearch + Kibana (future, behind `ISearchService` abstraction) |

### Configuration (appsettings.json)

```json
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Port=5432;Database=englishpartner;Username=postgres;Password=postgres",
    "Redis": "localhost:6379"
  },
  "GeminiAI": {
    "ApiKey": "<from-user-secrets>",
    "ModelName": "gemini-3.1-flash-lite-preview",
    "MaxTokens": 8192,
    "Temperature": 0.7
  },
  "Jwt": {
    "Secret": "<from-user-secrets>",
    "Issuer": "EnglishPartner",
    "Audience": "EnglishPartner.Client",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  },
  "FileStorage": {
    "BasePath": "./uploads",
    "BaseUrl": "/uploads",
    "MaxFileSizeMB": 10
  },
  "RateLimiting": {
    "AIChat": {
      "PermitLimit": 10,
      "WindowSeconds": 60
    },
    "AIScoring": {
      "PermitLimit": 50,
      "WindowSeconds": 3600
    }
  }
}
```

> **Note:** Sensitive values (`ApiKey`, `Jwt:Secret`) MUST use `dotnet user-secrets` in development. Never commit secrets to source control.

---

## 8. Cross-Module Communication Rules

1. **NEVER** reference another module's Domain, Application, or Infrastructure projects.
2. **ONLY** reference another module's `Contracts` project for Integration Events and public DTOs.
3. Use **Integration Events** for async communication between modules.
4. Use **Module Interfaces** (defined in Contracts) for synchronous queries across modules.
5. All integration event handlers must be **idempotent**.

### 8.1 Orchestration Pattern: AI & Learning Module (SM-2)
When users answer complex questions (e.g., Conversation, Writing, ExampleSentence), the evaluation relies on both the **AI Module** and **Learning Module**:
- **Evaluation Strategy:**
  - **Self-Evaluation:** For simple flashcards (Vocabulary, Grammar), the client sends a direct Quality Score (0-5) to the Learning Module without AI involvement.
  - **AI-Evaluation:** For complex inputs (Voice, Typing), the client or API orchestrator first calls the **AI Module** to analyze the input (grammar, accuracy). The AI returns a Quality Score (0-5).
- **Orchestration Flow:**
  - The AI Module acts as the **Examiner**; it returns the score.
  - The score is then passed to the **Learning Module** (`ItemMastery.Review(score)`), which acts as the **Scheduler** (calculating the Spaced Repetition interval via the SM-2 algorithm).
  - This flow avoids tight coupling between AI and Learning.

---

## 9. Database Rules

1. Each module owns its own **schema** in a shared PostgreSQL database.
2. Each module has its own **DbContext** — never share DbContext across modules.
3. **Foreign keys across modules are FORBIDDEN**. Use eventual consistency via events.
4. Store related module IDs as simple `Guid` properties (no navigation properties across modules).
5. Use **code-first migrations** per module.
6. Apply **global query filters** for `IsDeleted == false` on all `AggregateRoot` entities.
7. Use **`AuditableEntityInterceptor`** to auto-populate `CreatedAt/CreatedBy/UpdatedAt/UpdatedBy`.
8. Soft-delete entities via `SoftDelete()` method — **never hard-delete AggregateRoot entities**.
9. Each `DbContext` MUST configure `MigrationsHistoryTable` to its own schema — never let it default to `public`.

```csharp
// ✅ Correct — migration history stays in the module's own schema
options.UseNpgsql(connectionString,
    npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", MyModuleDbContext.Schema));

// ❌ Wrong — history leaks into public schema, breaking module isolation
options.UseNpgsql(connectionString);
```

### 9.1 Migration File Rules

> **⚠️ NEVER delete files inside any module's `Migrations/` folder.**

- Migration files are the full audit trail of database schema changes.
- To undo the latest unapplied migration use: `dotnet ef migrations remove` — never manually delete.
- To regenerate all migrations: only as a deliberate team decision (requires dropping all module schemas in DB first).

```
# Protected folders — DO NOT delete files here:
src/Modules/AI/.../Persistence/Migrations/
src/Modules/Curriculum/.../Persistence/Migrations/
src/Modules/Identity/.../Persistence/Migrations/
src/Modules/Learning/.../Persistence/Migrations/
src/Modules/Notifications/.../Persistence/Migrations/
```

### 9.2 Migration Commands (reference)

```bash
# Add a new migration for a module
dotnet ef migrations add <Name> \
  --project src/Modules/<Module>/...Infrastructure \
  --startup-project src/Apps/EnglishPartner.Api \
  --context <Module>DbContext

# Apply pending migrations via CLI (production)
dotnet ef database update \
  --project src/Modules/<Module>/...Infrastructure \
  --startup-project src/Apps/EnglishPartner.Api \
  --context <Module>DbContext
```

---

## 10. Caching Strategy (Redis)

| Cache Type | Use Case | TTL |
|---|---|---|
| **Query Cache** | Vocabularies, Grammar, Tenses (read-heavy) | 15–60 min |
| **User Session** | Current user profile, permissions | 30 min |
| **Leaderboard** | Top users by XP | 5 min |
| **Daily Content** | Daily words, daily goals | 24 hours |
| **AI Rate Limiting** | Per-user API call counters | Sliding window |

**Cache invalidation**: Use **cache-aside pattern**. Invalidate on write operations via Domain Event handlers.

---

## 11. File Storage Rules

1. Use `IFileStorageService` abstraction in SharedKernel.
2. Implementation: `LocalFileStorageService` (stores at `/{DataRoot}/uploads/{module}/{entityType}/{entityId}/{filename}`).
3. Return relative URLs from storage service, resolve to full URLs in API layer.
4. Future: Swap implementation to `AzureBlobStorageService` or `S3StorageService` without changing any Application/Domain code.

---

## 12. Error Handling

1. Use the `Result<T>` pattern from SharedKernel — avoid throwing exceptions for business logic errors.
2. Domain exceptions are reserved for truly exceptional/invariant-violation scenarios.
3. Use `ProblemDetails` (RFC 9457) for all API error responses.
4. Global exception handler middleware for unhandled exceptions.

---

## 13. Security

1. All endpoints require authentication unless explicitly marked `[AllowAnonymous]`.
2. Use **permission-based authorization** (not just role-based).
3. Admin endpoints require both the `Admin` role AND specific permissions.
4. JWT tokens include user ID, roles, and permissions as claims.
5. API keys for Gemini are stored in configuration (User Secrets for dev, environment variables for prod).

---

## 14. Multi-Language Expansion Strategy

The system is designed for **English first** but architecturally supports any language:

### What exists now
- `Language` entity in Curriculum module (seeded with English).
- All content entities have a `LanguageCode` field (default: `"en"`).
- `UserProfile.TargetLanguage` tracks what language the user is learning.
- AI prompts include `targetLanguage` parameter.

### What to add for a new language (e.g., Chinese)
1. **Seed new Language record** (`zh`, "Chinese", with language-specific config).
2. **Create language-specific content**: Add vocabulary, grammar rules, etc. with `LanguageCode = "zh"`.
3. **Language-specific features** (optional module extensions):
   - Chinese: Pinyin, Tone marks, Stroke order
   - Japanese: Kanji/Hiragana/Katakana modes, JLPT levels
   - Korean: Hangul system, TOPIK levels
4. **AI prompt templates**: Create templates per language for scoring, content generation.
5. **UI/Client**: Filter content by `LanguageCode`, add language selector.

### Architecture pattern
```
// All content queries include language filter
GET /api/v1/curriculum/vocabularies?languageCode=en
GET /api/v1/curriculum/vocabularies?languageCode=zh

// UserProfile stores target language
{
    "targetLanguage": "en",
    "nativeLanguage": "vi"
}
```

### NO code changes needed for:
- Auth / Identity (language-agnostic)
- Learning / Progress tracking (tracks by content ID, language-agnostic)
- Gamification (XP, achievements — language-agnostic)
- Notification (template-based, language-agnostic)
- AI Module (prompt-template-driven, add new templates per language)
