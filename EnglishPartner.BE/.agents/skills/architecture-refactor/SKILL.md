---
name: architecture-refactor
description: Plan or execute a scoped refactor toward Clean Architecture with DDD Aggregates and Feature-Based CQRS in this backend, preserving public and persisted compatibility.
---

# Architecture refactor

Read [AGENTS.md](../../../AGENTS.md), [target architecture](../../architecture.md) and the [2026-10-28 review](../../../../docs/reviews/2026-10-28-security-performance-architecture.md) with its fix order and [ADRs](../../../../docs/adr/README.md). The initial 2026-10-15 request is review/docs only; implementation requires a later implementation task. Follow the current user's scope rather than treating the plan as execution authority.

Choose a concrete use-case slice and resolve only the decisions it depends on. Inspect current status/diffs, preserve pre-existing tracked/untracked work and remember FE shares the Git root. Record baseline checks and compatibility before moving anything.

Keep mechanical moves distinguishable from behavior/schema corrections. Preserve project names, Entity suffixes, SQL mappings, IDs, migrations, routes, JSON fields and hub protocols unless the task explicitly changes them. Do not combine wholesale type renaming, new frameworks and business fixes into one structural pass.

Use Learning as the proposed repository/UoW and aggregate-write example. Migrate callers before removing old abstractions; do not leave competing default write paths. The singleton realtime manager cannot directly adopt a scoped repository without introducing operation scopes.

Verify in proportion to impact: compile/DI for moves, focused invariant/use-case checks for behavior, relational checks for SQL guarantees, transport checks for endpoint/hub contracts. Do not broaden into unrelated tests or live provider/database writes. Stop scope expansion at newly discovered unrelated defects and report them separately.

For Modular Monolith work (ADR-0006: moving code into modules, Contracts, module DbContext/schema, outbox), also follow the [modular-monolith skill](../modular-monolith/SKILL.md); its phase gates and move-only rule take precedence for those tasks.

For plan-only requests, deliver file/use-case mapping, order/dependencies, compatibility/schema risks, unresolved decisions and acceptance gates without source edits. For implementation, report actual diffs/check results and remaining gates; never mark a documented phase complete without implementation evidence.
