# Backend agent guide

## Scope and current state

This guide applies to `EnglishPartner.BE`. The Git root is its parent, `EnglishPartner`; the frontend is a sibling and is outside backend-only work.

The solution and namespaces currently use **EnglishLiveTutor**, target **.NET 10**, and contain four production projects. Do not rename them to match the workspace directory as part of an unrelated task.

Architecture target: **Modular Monolith** ([ADR-0006](../docs/adr/0006-modular-monolith.md)) whose modules are internally **Pragmatic Clean Architecture with DDD Aggregates and Feature-Based CQRS**. The migration is phased (P0–P4 in the ADR); check the current phase in `docs/tasks/20261101-modular-monolith/plan.md` before moving code. The target is a convention for future changes, not a claim that existing code already complies.

- [Modular Monolith decision and module map](../docs/adr/0006-modular-monolith.md) — overrides the four-project shape and single `AppDbContext` in the architecture doc below.
- [Architecture decisions and target structure](.agents/architecture.md).
- [Current review and fix checklist, 2026-10-28](../docs/reviews/2026-10-28-security-performance-architecture.md).
- [Architecture decision records](../docs/adr/README.md).
- Shared rules, roles and task workflow for all agents: [root AGENTS.md](../AGENTS.md).

Implementation follows the task workflow in the root guide: a task `plan.md` under `docs/tasks/` defines the authorized scope.

## Working rules

- Read `git status --short` before work. Preserve existing tracked changes and untracked files. Scope Git commands to this backend when appropriate.
- For review or plan-only requests, keep source, project files, migrations, configuration and tests unchanged. Create only requested guidance/review artifacts.
- Treat the source as evidence of current behavior; distinguish it from proposed architecture and from runtime verification. Report findings in Vietnamese with file locations, trigger, consequence and priority.
- Preserve routes, HTTP JSON fields, pagination headers, SignalR hub methods/events, persisted IDs, table/column names and migration history during structural moves. Behavioral or schema corrections belong to explicit, separately reviewable changes.
- Do not print credentials, request API keys, tokens, connection strings or complete provider payloads. Inspect configuration keys without exposing their values.
- Build/test only as appropriate to the requested change. For documentation-only work, validate links, skill frontmatter and whitespace; do not start the API, seed/migrate SQL or call a paid AI provider.

## Layer boundaries

```text
Api -> Application
Api -> Infrastructure                 composition root and adapter registration
Application -> Domain
Infrastructure -> Application + Domain
Domain -> BCL only
```

Module boundaries (ADR-0006) cut across these layers. Until a module is its own project (phase P3), the module of a file is given by the mapping table in the [modular-monolith skill](.agents/skills/modular-monolith/SKILL.md) and enforced by the architecture tests:

```text
Module internals -> own module + Shared Kernel + other modules' Contracts only
Contracts        -> Shared Kernel only (no EF, no handlers, no entities)
Api host         -> modules (composition, endpoint/hub mapping)
```

- **Domain:** aggregate roots, owned child entities, invariants, meaningful value objects and write repository contracts. No EF, HTTP, SignalR, DI or provider SDK types. Put children next to their owning aggregate; do not create a repository per child/table.
- **Application:** use cases under `Features/<Feature>/Commands/<UseCase>` or `Queries/<UseCase>`. Orchestrate ownership checks, domain behavior and one explicit commit boundary. Keep feature contracts beside their feature; reserve `Common` for genuinely shared abstractions.
- **Queries:** read-only and return named DTOs. EF Core LINQ/projections through an Application-owned read abstraction are an accepted pragmatic choice. They must preserve root visibility and user scope when reading child tables.
- **Commands:** load write aggregates through purpose-specific repositories, invoke domain behavior, and commit through a shared unit of work. New writes must not copy the current pattern of editing child `DbSet`s through a separate factory context. Migrate legacy paths incrementally.
- **Infrastructure:** implement persistence and external provider ports. Keep EF mappings/migrations, websocket protocol, HTTP clients and storage here. A repository persists a decision; it should not decide user-facing workflow or return anonymous HTTP models.
- **API:** bind, authenticate/authorize, invoke an Application use case/port, map result to transport. Infrastructure references for registration are expected; hubs/endpoints calling provider managers or mutating EF directly are migration debt.

CQRS runs on **MediatR** ([ADR-0001](../docs/adr/0001-keep-mediatr.md)). `ICommand`/`IQuery`/`ICommandHandler`/`IQueryHandler` in `Common/Interfaces/ICqrs.cs` are thin wrappers over `IRequest`/`IRequestHandler`; use them for new handlers. Dispatch only through `ISender` so pipeline behaviors apply; do not inject a handler into another handler — extract shared logic into an Application service. Module projects/Contracts projects and the outbox described in ADR-0006 are authorized by that ADR; a generic repository, an external message broker or a separate read database still require a concrete need.

## Behavioral rules to preserve or establish in scoped fixes

- Authenticated claims determine the learner identity. Do not introduce new `default-learner` or `X-User-Id` fallbacks for personal data. Existing fallbacks are documented defects/debt, not examples to extend.
- Verify ownership before returning or changing a learner's plan, review, assessment or realtime session. Audit `CreatedBy` is not proof of ownership.
- A query must not call a command or persist state. Migrating the existing `/today` initialization behavior requires the compatibility decision in the plan.
- Business atomicity can span a few aggregates **of the same module** in one commit. Across modules there is no shared transaction (ADR-0006): use an outbox event with an idempotent handler, or fix the boundary if the use case truly needs atomicity.
- Database constraints/concurrency checks protect correctness across processes. An in-memory semaphore only coordinates one process.
- Preserve `IsActive`/`IsEnabled` separately from `IsDeleted`. Soft delete of a root must not cancel deletion of another aggregate's children or expose children through direct queries.
- Keep provider calls outside long SQL transactions. Validate provider outputs before domain state changes; retain reference/rubric snapshots where history needs them.

## Local skills

Read the relevant skill when its task applies; these are project-specific workflows for the stack already present, not copies of every global skill installed on the machine.

| Task | Skill |
| --- | --- |
| Review architecture and trace a backend flow | [backend-architecture-review](.agents/skills/backend-architecture-review/SKILL.md) |
| Design an aggregate, entity or invariant | [domain-aggregate-design](.agents/skills/domain-aggregate-design/SKILL.md) |
| Add/rework a command, query or feature contract | [feature-cqrs](.agents/skills/feature-cqrs/SKILL.md) |
| Change EF persistence, commit boundaries, adapters or migrations | [infrastructure-persistence](.agents/skills/infrastructure-persistence/SKILL.md) |
| Change HTTP/SignalR transport and Application integration | [api-realtime](.agents/skills/api-realtime/SKILL.md) |
| Plan or execute an authorized architecture slice | [architecture-refactor](.agents/skills/architecture-refactor/SKILL.md) |
| Move code into a module, add Contracts, module DbContext/schema, outbox, architecture tests | [modular-monolith](.agents/skills/modular-monolith/SKILL.md) |
| Query performance, pagination, indexes, background workers | [ef-performance](.agents/skills/ef-performance/SKILL.md) |
| Call Gemini/AGY providers, BYO key handling, AI cost and output validation | [ai-provider-integration](.agents/skills/ai-provider-integration/SKILL.md) |
| Write or run backend tests, including cross-user authorization tests | [backend-testing](.agents/skills/backend-testing/SKILL.md) |
| Security review of a change (shared) | [security-audit](../.agents/skills/security-audit/SKILL.md) |
| SignalR hub and live session lifecycle (shared) | [realtime-session](../.agents/skills/realtime-session/SKILL.md) |
| HTTP/hub contract changes that touch the FE (shared) | [api-contract-sync](../.agents/skills/api-contract-sync/SKILL.md) |

## Verification and handoff

For an authorized implementation, choose relevant checks from the refactor plan. State which commands actually ran, results, and any SQL/provider/HTTP checks skipped. A successful build or EF InMemory test does not establish SQL Server, authentication, realtime or Google provider behavior.

Leave a concise handoff: findings or changes, affected contracts, verification evidence and remaining decisions. Never mark a planned phase as implemented based on documentation alone.
