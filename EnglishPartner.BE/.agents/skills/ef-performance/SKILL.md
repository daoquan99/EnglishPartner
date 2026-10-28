---
name: ef-performance
description: Design, change or review query and runtime performance in this SQL Server backend — pagination limits, projections, Include depth, indexes, search predicates, refresh-token growth, background workers and log volume. Use when a change touches list endpoints, EF queries, hosted services or hot realtime write paths.
---

# EF Core and runtime performance

Read [AGENTS.md](../../../AGENTS.md), root rules 6–8 in [../AGENTS.md](../../../../AGENTS.md) and the P items in the [2026-10-28 review](../../../../docs/reviews/2026-10-28-security-performance-architecture.md). Judge the changed query, not the whole repository.

## Rules

- **Pagination:** every list returns a bounded page. Default page size (20) and a hard maximum (100) apply even when the client sends no `page`/`pageSize`; `PagedResultDto.CreateAsync`/`CreateProjectedAsync`/`FromPage` must not fall back to "all rows". Keep the `X-Total-*` headers and `paged` behaviour stable or treat the change as a contract change ([api-contract-sync](../../../../.agents/skills/api-contract-sync/SKILL.md)).
- **Shape:** list queries project to DTOs with `Select`; load aggregates with `Include` only for writes or single-item detail. Two-phase "page ids then load details" is fine; six nested `Include` branches per list page is not.
- **Tracking:** `AsNoTracking` for reads; split queries are the default (`UseQuerySplittingBehavior.SplitQuery`), so check the round-trip count of deep includes.
- **Search:** do not wrap columns in `ToLower()`/`ToUpper()` — SQL Server CI collation already ignores case and the function blocks index use. Prefer prefix matches on indexed columns; leading-wildcard `Contains` on large tables needs Full-Text Search or a documented accepted cost.
- **Indexes:** a new filter/sort used by a hot query needs a supporting index in the entity configuration plus a migration; composite order follows equality-then-range.
- **Unbounded children:** never `Include(u => u.RefreshTokens)`/messages to find one row; query the child table directly by indexed key. Growth tables need a retention job.
- **Writes:** no process-wide lock (`SemaphoreSlim` in a Singleton) for multi-user data; rely on unique indexes and handle `DbUpdateException` for races. Batch or debounce high-frequency realtime writes.
- **Provider calls:** never inside an open transaction; the Gemini HTTP call completes before `ExecuteInTransactionAsync`.
- **Background work:** polling intervals match the business need (or compute the next due time); per-tick logging at `Debug`, not `Information`. `LoggingBehavior` logs every MediatR request, so high-frequency internal commands must not flood logs.

## Verification

Read the generated SQL for the changed query (`ToQueryString()` in a test or EF logging at `Debug`) and state the number of round trips and whether an index supports it. InMemory tests prove behaviour only, not SQL performance — say so in the report and list what needs a SQL Server check.
