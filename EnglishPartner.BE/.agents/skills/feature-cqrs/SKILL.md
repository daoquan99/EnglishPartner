---
name: feature-cqrs
description: Design, implement or review feature-based commands, queries and DTO contracts in EnglishLiveTutor.Application, including aggregate writes and pragmatic EF read projections.
---

# Feature-based CQRS

Read [AGENTS.md](../../../AGENTS.md) and [Application decisions](../../architecture.md#3-application-and-cqrs). Keep the existing lightweight CQRS interfaces unless a task establishes a concrete need to replace them.

Place each use case under `Features/<Feature>/Commands/<UseCase>` or `Queries/<UseCase>`. Small request/handler pairs may share one file. Feature DTOs/ports/mappings stay in their feature; Common holds genuinely shared capabilities. Avoid multi-use-case files like the current AdminIdentityCrudCommands.

Commands validate input and ownership, load aggregates through write repositories, invoke domain behavior, stage related history, and commit through a shared scoped UoW. Do not add a factory-created write context alongside repository state. A local transaction spanning plan/progress/attempt is acceptable. Keep provider calls outside long SQL transactions.

Queries are read-only, return named DTOs, and do not invoke commands. EF LINQ through an Application-owned read abstraction is allowed. Apply user and root visibility predicates when reading children. Batch page-related data rather than running role/permission/card queries for each row.

Inspect `/today` auto-init and review identity/retry behavior before reusing their current handlers as examples. Treat `default-learner`, object responses and mixed persistence as migration debt, not new-code conventions.

Preserve HTTP JSON/paging and SignalR contracts when relocating DTOs. If behavior must change, list that explicitly and use the relevant [plan slice](../../../docs/backend-refactor-plan.md). When implementation is authorized, verify handler DI and the meaningful behavior changed; distinguish handler tests from HTTP/SQL verification.
