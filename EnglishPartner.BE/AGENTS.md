# Backend agent guide

## Scope and current state

This guide applies to `EnglishPartner.BE`. The Git root is its parent, `EnglishPartner`; the frontend is a sibling and is outside backend-only work.

The solution and namespaces currently use **EnglishLiveTutor**, target **.NET 10**, and contain four production projects. Do not rename them to match the workspace directory as part of an unrelated task.

Architecture target: **Pragmatic Clean Architecture with DDD Aggregates and Feature-Based CQRS**. The target is a convention for future changes, not a claim that existing code already complies.

- [Architecture decisions and target structure](.agents/architecture.md).
- [Source-based review, 2026-10-15](docs/backend-architecture-review-2026-10-15.md).
- [Phased refactor plan](docs/backend-refactor-plan.md).

The initial request authorizes review and documentation only. Do not start the refactor until a subsequent implementation request. A later explicitly authorized task can change source within its own scope; this guide does not require repeated permission.

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

- **Domain:** aggregate roots, owned child entities, invariants, meaningful value objects and write repository contracts. No EF, HTTP, SignalR, DI or provider SDK types. Put children next to their owning aggregate; do not create a repository per child/table.
- **Application:** use cases under `Features/<Feature>/Commands/<UseCase>` or `Queries/<UseCase>`. Orchestrate ownership checks, domain behavior and one explicit commit boundary. Keep feature contracts beside their feature; reserve `Common` for genuinely shared abstractions.
- **Queries:** read-only and return named DTOs. EF Core LINQ/projections through an Application-owned read abstraction are an accepted pragmatic choice. They must preserve root visibility and user scope when reading child tables.
- **Commands:** load write aggregates through purpose-specific repositories, invoke domain behavior, and commit through a shared unit of work. New writes must not copy the current pattern of editing child `DbSet`s through a separate factory context. Migrate legacy paths incrementally.
- **Infrastructure:** implement persistence and external provider ports. Keep EF mappings/migrations, websocket protocol, HTTP clients and storage here. A repository persists a decision; it should not decide user-facing workflow or return anonymous HTTP models.
- **API:** bind, authenticate/authorize, invoke an Application use case/port, map result to transport. Infrastructure references for registration are expected; hubs/endpoints calling provider managers or mutating EF directly are migration debt.

Keep the existing lightweight `ICommandHandler` / `IQueryHandler`; a mediator package, event bus, generic repository, separate read database or additional projects require a concrete need.

## Behavioral rules to preserve or establish in scoped fixes

- Authenticated claims determine the learner identity. Do not introduce new `default-learner` or `X-User-Id` fallbacks for personal data. Existing fallbacks are documented defects/debt, not examples to extend.
- Verify ownership before returning or changing a learner's plan, review, assessment or realtime session. Audit `CreatedBy` is not proof of ownership.
- A query must not call a command or persist state. Migrating the existing `/today` initialization behavior requires the compatibility decision in the plan.
- Business atomicity can span a few aggregates in the same SQL database when the use case needs it. Share one context/commit; do not introduce distributed events to avoid a local transaction.
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

## Verification and handoff

For an authorized implementation, choose relevant checks from the refactor plan. State which commands actually ran, results, and any SQL/provider/HTTP checks skipped. A successful build or EF InMemory test does not establish SQL Server, authentication, realtime or Google provider behavior.

Leave a concise handoff: findings or changes, affected contracts, verification evidence and remaining decisions. Never mark a planned phase as implemented based on documentation alone.
