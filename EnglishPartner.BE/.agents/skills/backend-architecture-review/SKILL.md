---
name: backend-architecture-review
description: Review this EnglishPartner backend across Domain, Application, Infrastructure and API using source evidence, aggregate ownership and CQRS flow tracing. Use for architecture assessments and refactor planning.
---

# Backend architecture review

Read the root [AGENTS.md](../../../AGENTS.md) and [architecture decisions](../../architecture.md). Use the [2026-10-28 review](../../../../docs/reviews/2026-10-28-security-performance-architecture.md) as the current checklist, then verify current code before repeating findings.

For review-only requests, keep source/configuration/tests/migrations unchanged. Guidance/report files may be created when requested. Do not start the API: its current startup attempts SQL migration and seeding.

Inventory project references, feature folders, DI lifetimes, aggregate roots/children, repository contracts, read/write paths and transport adapters. Trace representative flows end to end, especially daily learning, generation/import, pronunciation, identity and realtime.

Inspect both source dependencies and actual behavior: query-to-command calls, direct child writes, multiple contexts/commits, actor/owner checks, root visibility, retry/concurrency, and provider orchestration. Accept EF projections in Application queries; distinguish that deliberate choice from bypassing aggregate invariants on writes.

Report in Vietnamese, Domain -> Application -> Infrastructure -> API, with severity, exact file/symbol/line, trigger, consequence, evidence level and a narrow proposed change. Separate confirmed source facts, runtime risks and unresolved product decisions. Record coverage and what was not run; do not claim every line was audited from inventory searches alone.

For a plan, follow the fix order in that review and the task template in [docs/tasks](../../../../docs/tasks/README.md) and include contract/schema impact and acceptance gates. Do not turn the review into implementation or reformat unrelated files.
