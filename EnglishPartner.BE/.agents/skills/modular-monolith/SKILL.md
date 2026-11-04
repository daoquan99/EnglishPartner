---
name: modular-monolith
description: Move EnglishPartner.BE code into Modular Monolith modules (ADR-0006) — file-to-module mapping, Contracts, architecture boundary tests, per-module DbContext interfaces and real contexts, schema transfer migrations and the outbox. Use for any task in docs/tasks/20261101-modular-monolith or one that adds a cross-module dependency.
---

# Modular Monolith

Read [ADR-0006](../../../../docs/adr/0006-modular-monolith.md), the [root AGENTS.md](../../../../AGENTS.md) rules 13–15, [BE AGENTS.md](../../../AGENTS.md) and the roadmap `docs/tasks/20261101-modular-monolith/plan.md` for the current phase. Do only the phase and module your prompt names.

## Project layout (target, P5)

```text
src/Shared/EnglishLiveTutor.Shared.{Domain,Application,Infrastructure,Presentation}   building blocks (namespaces unchanged: EnglishLiveTutor.Domain.Common…, EnglishLiveTutor.Application.Common…)
src/Modules/<X>/EnglishLiveTutor.Modules.<X>.{Domain,Application,Infrastructure,Presentation,Contracts}
src/EnglishLiveTutor.Api              host + composition root: Program, auth, CORS, rate limit, Map<X>Endpoints, MapHub; Composition/Application (ApplicationAssemblies, Add<X>Module, MediatR/validators) and Composition/Infrastructure (Add<X>Infrastructure, migration runner, seeder, idempotency service)
```

Allowed references per layer are enforced by `tests/.../Architecture/ProjectReferenceTests.cs`: `.Domain` → `Shared.Domain` only (no packages); `.Contracts` → `Shared.Domain`, `Shared.Application`, Contracts in the DAG; `.Application` → own Domain + Contracts, Shared, Contracts in the DAG; `.Infrastructure` → own Application/Domain/Contracts, `Shared.Infrastructure`, Shared, Contracts in the DAG; `.Presentation` → own Application/Contracts, `Shared.Presentation`, `Shared.Application`.

## Module map

Until a module has its own project (P3), a source file belongs to the module of its path. A path not listed here is **Shared Kernel** only if listed as such; anything else unclear → stop and report. **The authoritative mapping is `tests/EnglishLiveTutor.Domain.Tests/Architecture/ModuleMap.cs`**; this table is a summary (state after P2, 2026-11-01). A file under `src/Modules/<X>/` belongs to module X; everything under `src/Shared/` is Shared.

| Module | Domain (`EnglishLiveTutor.Domain/`) | Application (`Features/`) | Infrastructure | Api |
| --- | --- | --- | --- | --- |
| Identity | `Identity/` | `Identity/` | `Identity/`, `Repositories/IdentityRepositories.cs`, `Persistence/Configurations/IdentityConfigurations.cs` | `Endpoints/IdentityEndpoints.cs`, `Services/ClaimsPermissionChecker.cs` |
| Catalog | `AiCatalog/`, `Prompts/`, `Voices/`, `Topics/` | `AiModels/`, `Prompts/`, `Voices/`, `Topics/` | `Repositories/{AiModelCatalog,PromptConfiguration,SpeechSpeed,TeacherVoice,Topic}Repository.cs`, `Prompts/` (seed defaults), configurations `AiModelConfig`, `Prompt`, `TeacherVoice`, `Topic` | `Endpoints/{AiModel,Prompt,Voice,Topic}Endpoints.cs` |
| Speech | `Pronunciation/` | `Pronunciation/` | `Repositories/{AudioRecording,GeneratedPronunciationAudio,PronunciationAssessment,ScoringRubric}Repository.cs`, `Ai/GeminiLivePronunciationTtsSynthesizer.cs`, `Ai/GeminiPronunciationAiAssessor.cs`, `Ai/PronunciationSynthesisLock.cs`, `Persistence/LessonRubricSeeder.cs`, configuration `Pronunciation` | `Endpoints/{Pronunciation,LessonRubric}Endpoints.cs` |
| Lessons | `Lessons/`, `LessonAttempts/` | `Lessons/` | `Lessons/`, `Storage/`, `Repositories/LessonRolePlayRepository.cs`, `Ai/{GeminiLessonAiGenerator,GeminiRolePlayReplyProvider,LessonAiOptions}.cs`, configurations `Lesson`, `LessonRolePlay` | `Endpoints/{Lesson,LessonAttempt,LessonMedia}Endpoints.cs` |
| Grammar | `Grammar/` | `Grammar/` | `Grammar/`, `Ai/{GeminiGrammarExerciseAiGenerator,GrammarGenerationAiOptions}.cs`, configuration `Grammar` | `Endpoints/{Grammar,AdminGrammar}Endpoints.cs` |
| Vocabulary | `Vocabulary/`, `Learning/`, `VocabularyGames/` | `Vocabulary/`, `Learning/`, `VocabularyGames/` | `VocabularyGames/`, `Repositories/{DailyVocabularyPlan,FallingWordGame,UserVocabularyProgress,Vocabulary}Repository.cs`, `Ai/{GeminiVocabularyAiGenerator,VocabularyGenerationContent,VocabularyGenerationOptions}.cs`, configurations `Vocabulary`, `Learning`, `FallingWordGame` | `Endpoints/{Vocabulary,DailyLearning,VocabularyGames}Endpoints.cs` |
| LiveTutor | `Conversations/` | `Conversations/` (incl. `Queries/PreviewSystemPrompt`) | `Realtime/`, `Repositories/ConversationRepository.cs`, `Ai/{CefrPromptBuilder,GrammarCorrectionAnalyzerService,FastTextTranslatorFallback}.cs`, configuration `Conversation` | `Hubs/`, `Realtime/`, `Endpoints/ConversationEndpoints.cs` |
| Shared Kernel | `Common/` (incl. `Common/Exercises/` — `ExerciseType`, `GradingMode`, `SubQuestionKind` shared by Lessons, Grammar, Speech rubrics) | `Application/Common/` (except module-specific files below), `Application/Modules/IModuleDbContext*.cs` | `Persistence/{AppDbContext,UnitOfWork,IdempotencyService,DatabaseSeeder}.cs`, `Persistence/Interceptors/`, `Persistence/Entities/`, `Configurations/{EntityConfigurationExtensions,IdempotencyConfigurations}.cs`, `Ai/{GeminiHttpHelper,GeminiApiKeyResolver,AgyCliRunner,AntigravityCliOptions}.cs`, `DependencyInjection.cs` | `Program.cs`, `Common/`, `Configuration/`, `Exceptions/`, `Security/`, `Services/HttpContextCurrentUser.cs`, `Endpoints/{Health,PaginationEndpointExtensions}.cs` |

Speech also owns lesson rubric CRUD in `Features/Pronunciation/Rubrics/` (moved from Lessons in P2). Module-specific files still in `Application/Common` (move into the owning module project in P3): `Models/{IdentityDtos}` → Identity; `Models/{PromptAndVoiceDtos,TopicDtos}` → Catalog; `Interfaces/ICefrPromptBuilder` → LiveTutor; `Interfaces/IPronunciationSynthesisLock` → Speech; `Models/{LessonDtos,LessonAttemptDtos}`, `Interfaces/{ILessonAiGenerator,ILessonCandidateValidator,ILessonMediaStorage,ILessonPromptBuilder,ILessonServices,IRolePlayReplyProvider}`, `Exercises/` → Lessons; `Models/{VocabularyDtos,ManualVocabularyGenerationDtos}`, `Interfaces/IVocabularyGenerationContent` → Vocabulary; `Models/LiveSessionDtos`, `Interfaces/ILiveSessionRealtimeNotifier` → LiveTutor. `Models/AdminCrudDtos.cs` and `Interfaces/IAiServices.cs` are mixed: split per module. A file that turns out to serve several modules stays in Shared Kernel only if it has no module-specific types.

Allowed dependencies (DAG): Speech → Catalog; Lessons → Speech, Catalog; Grammar → Lessons, Speech; Vocabulary → Speech, Catalog; LiveTutor → Catalog. Every module → Shared Kernel and → Identity (Contracts only). Speech must not depend on Vocabulary: it declares ports in its Contracts that Vocabulary implements.

## Contracts

- Location before P3: `Features/<FeatureOfModule>/Contracts/`. After P3: project `EnglishLiveTutor.Modules.<X>.Contracts`. An enum used by several modules that conceptually belongs to none (e.g. `ExerciseType`) goes to Shared Kernel `Domain/Common/<Concept>/`, never duplicated as constants.
- A port implemented by another module (dependency inversion, e.g. Speech's `IPronunciationTargetTextSource` implemented by Vocabulary) is declared in the **consuming** module's Contracts.
- Contains only: interfaces of read services/ports another module calls, DTO records, enums shared across modules, integration event records. No EF types, no entities, no handlers, no `IAppDbContext`.
- Implementation of a Contracts interface lives in the owning module and is registered in that module's DI extension.
- Keep moved enums' numeric values and names; they may be persisted.

## Architecture tests (P1)

`tests/EnglishLiveTutor.Domain.Tests/Architecture/ModuleBoundaryTests.cs`, no new package:

- Map each `.cs` under `src/` to a module using the table above (path-prefix rules in one `ModuleMap` class).
- Parse `using EnglishLiveTutor.…` directives and fully-qualified `EnglishLiveTutor.…` references; resolve the target namespace to a module.
- Fail on a reference from module A to module B internals when B is not A, not Shared Kernel and the reference is not to `…Contracts…`; fail on a dependency not in the DAG; fail when a Contracts file references EF or an entity.
- Known violations live in `Architecture/module-boundary-allowlist.txt` (`<source file> -> <target namespace>`, one per line). The test also fails when an allowlisted entry no longer occurs, so the list can only shrink.

## Per-module database access

- P1: add `I<Module>DbContext` (in the module's Application area) exposing only that module's `DbSet`s; `AppDbContext` implements all of them; register each as the same scoped instance. Move handlers from `IAppDbContext` to their module interface. `IUnitOfWork` stays shared until P4.
- P4 (needs user confirmation and a backup, rule 12):
  1. `<Module>DbContext : DbContext` with `HasDefaultSchema("<module>")` and `MigrationsHistoryTable("__EFMigrationsHistory", "<module>")`; apply only that module's configurations.
  2. Baseline migration per context with empty `Up()/Down()` while tables are still in `dbo` — generated against the existing model, never rewriting the 21 legacy migrations.
  3. Separate migration per module: `ALTER SCHEMA <module> TRANSFER dbo.<Table>` for each table, `Down()` transfers back. Metadata only; no data copy.
  4. Verify on a disposable SQL Server (LocalDB/container restored from backup) before the real DB: row counts per table equal before/after, app starts, `dotnet ef migrations list` shows each context up to date.
  5. Unit of work per module; a command commits only its module's context.

## Outbox (P4, or earlier only when a use case needs it)

- Table `messaging.OutboxMessages` (Id, Type, Payload JSON, OccurredAt, ProcessedAt, Attempts, Error). Written by the producing module inside its own commit.
- Hosted service reads unprocessed rows in small batches, dispatches to handlers, marks processed; retries with backoff, caps attempts, logs without payload secrets.
- Handlers are idempotent (record processed message Id per consumer or use natural idempotency). No AI provider call while holding a transaction.

## Moving code (P2/P3)

- Move-only: change `namespace` only if the prompt says so (default: **keep namespaces** during P3 project moves), `using`, `public`→`internal`. No logic, signature or name changes.
- Keep EF table names: if an entity's table name currently comes from the `DbSet` property name, add an explicit `ToTable("<current name>")` before moving it to another context.
- Before and after: `dotnet build EnglishLiveTutor.slnx` and `dotnet test EnglishLiveTutor.slnx` in `EnglishPartner.BE`; pass/fail counts must equal the baseline in the plan (known failures: `PronunciationSynthesisAudioCacheTests`).
- `dotnet ef migrations has-pending-model-changes` (or adding a throwaway migration and discarding it) must show no model change for P1–P3.
- Report: files moved (`git diff -M --stat`), allowlist size before/after, build/test counts.
