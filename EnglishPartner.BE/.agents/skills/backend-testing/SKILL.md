---
name: backend-testing
description: Write, extend or run backend tests across modular monolith test projects (xUnit, EF InMemory) — domain invariants, MediatR handlers through the pipeline, endpoint/HTTP integration, and mandatory cross-user authorization tests for security fixes. Use when a backend change needs test evidence.
---

# Backend testing

Read [AGENTS.md](../../../AGENTS.md). The backend test suite is separated by module and architectural concerns (net10.0, xUnit 2.9, `Microsoft.EntityFrameworkCore.InMemory`, coverlet):

```text
tests/
  EnglishLiveTutor.Tests.Common/                    class library: Infrastructure/ApiTestFactory.cs, TestDoubles/**, LessonTestDoubles.cs, helper dùng chung
  EnglishLiveTutor.ArchitectureTests/               kiến trúc, module map, boundary scanner & allowlist, project references, persistence/migration guard
  EnglishLiveTutor.Api.IntegrationTests/            test qua WebApplicationFactory/HTTP (endpoints, ProblemDetails, auth/JWT qua HTTP, rate limit, composition DI)
  Modules/<X>/EnglishLiveTutor.Modules.<X>.Tests/   domain + handler + infrastructure + cross-user security của từng module X (Catalog, Grammar, Identity, Lessons, LiveTutor, Speech, Vocabulary)
```

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
dotnet test EnglishLiveTutor.slnx                                  # in EnglishPartner.BE
dotnet test EnglishLiveTutor.slnx --filter "FullyQualifiedName~Ownership"
```

## Limits to report

InMemory does not enforce relational constraints, transactions, unique indexes the same way, raw SQL, collation or query performance. A green run does not verify SQL Server, migrations, authentication middleware end-to-end, SignalR or providers — list what remains unverified in `report.md`.
