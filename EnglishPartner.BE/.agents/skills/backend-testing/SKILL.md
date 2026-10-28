---
name: backend-testing
description: Write, extend or run backend tests in tests/EnglishLiveTutor.Domain.Tests (xUnit, EF InMemory) — domain invariants, MediatR handlers through the pipeline, endpoint behaviour, and mandatory cross-user authorization tests for security fixes. Use when a backend change needs test evidence.
---

# Backend testing

Read [AGENTS.md](../../../AGENTS.md). The only test project is `tests/EnglishLiveTutor.Domain.Tests` (xUnit 2.9, `Microsoft.EntityFrameworkCore.InMemory`, coverlet). Existing examples: `CqrsDependencyInjectionTests.cs`, `EndpointCqrsTests.cs`, `FallingWordGameCqrsPipelineTests.cs`, `DatabaseSeederAdminUserTests.cs`.

## What to test

- **Domain:** invariants and state transitions on the aggregate directly, no DbContext.
- **Handlers:** send through `ISender` from a real DI container built with `AddApplicationServices` + test doubles, so Authorization/Validation/Transaction behaviors run. Calling `HandleAsync` directly is acceptable only for pure logic and must not be the sole test of a security rule.
- **Security fixes (required):** for each ownership/IDOR fix add a test where user B requests user A's resource and gets not-found/forbidden, plus one where A succeeds. Write the failing test first and record that it failed before the fix.
- **Contracts:** when an endpoint's JSON or status changes, assert the shape (property names, status code, paging headers).
- **Providers:** fake `HttpMessageHandler`, `TimeProvider` fakes for time; never real Gemini, SQL Server or network.

## Conventions

- One behaviour per test, name `Method_Condition_Expected`. Arrange data with domain factory methods (`UserEntity.Create`, `LessonAttemptEntity.Start`), not by setting private state.
- Fresh InMemory database name per test (`Guid.NewGuid().ToString()`); no shared static state.
- Current user via a fake `ICurrentUser`/`IPermissionChecker`; never register `AllowAllPermissionChecker` in a test that asserts authorization.

## Run

```bash
dotnet test EnglishLiveTutor.sln                                  # in EnglishPartner.BE
dotnet test EnglishLiveTutor.sln --filter "FullyQualifiedName~Ownership"
```

## Limits to report

InMemory does not enforce relational constraints, transactions, unique indexes the same way, raw SQL, collation or query performance. A green run does not verify SQL Server, migrations, authentication middleware end-to-end, SignalR or providers — list what remains unverified in `report.md`.
