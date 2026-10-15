---
name: infrastructure-persistence
description: Review or change EF Core repositories, commit boundaries, auditing, soft delete, migrations, seeding and external adapters in this SQL Server backend.
---

# Infrastructure and persistence

Read [AGENTS.md](../../../AGENTS.md), [Infrastructure decisions](../../architecture.md#4-infrastructure) and the relevant R2/R3/R5 slice in [the plan](../../../docs/backend-refactor-plan.md).

Trace the actual DbContext instance and commit owner, not just repository names. Baseline scoped repositories, factory-per-operation Topic/Conversation repositories, and Application factory writes have different tracking semantics. New write repositories in the target share one scoped AppDbContext/UoW. A singleton session adapter creates operation scopes instead of capturing scoped repositories.

Before namespace/folder moves, record entity mappings, IDs, table/column/index names and migration identities. Do not rewrite existing migrations or run database updates as a consequence of a structural move. Compare generated model changes deliberately; do not suppress mismatch warnings to pass a check.

For soft delete, preserve only descendants of the specific deleted root. Verify unrelated child deletions still happen, direct-child reads respect root visibility, and IsActive/IsEnabled remain distinct from IsDeleted. Restore/unique-key behavior needs an explicit policy.

In-memory locks coordinate one process only. For cache writes, distinguish allowed duplicate versions from unique active entries; classify actual provider/database conflicts and expose best-effort failure through safe logging. Do not swallow all DbUpdateException as duplicate conflicts.

Separate migration, readiness and optional seed failure behavior. Test seeding against partial data as well as empty data, preserving administrator edits. A recording key must refer to real storage if retention is promised.

When implementation is authorized, select focused persistence checks. EF InMemory covers some state/mapping behavior but does not prove SQL Server uniqueness, transactions, migrations or concurrency. Run disposable SQL checks only in the authorized environment; report unverified gates plainly.
