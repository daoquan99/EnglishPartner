# EnglishLiveTutor Backend Architecture

## 1. Architectural Style: Pragmatic Clean Architecture + DDD + CQRS

The **EnglishLiveTutor** backend is structured using **Pragmatic Clean Architecture** combined with **Domain-Driven Design (DDD)** tactical patterns and **Feature-Based CQRS**.

> **Important:** This project is **not** a Modular Monolith. It uses the four core projects (`EnglishLiveTutor.Api`, `EnglishLiveTutor.Application`, `EnglishLiveTutor.Domain`, `EnglishLiveTutor.Infrastructure`) and the test project (`EnglishLiveTutor.Domain.Tests`) without introducing bounded-context subprojects, distributed event buses, or generic repository wrappers.

### Dependency Direction

```text
EnglishLiveTutor.Api  -->  EnglishLiveTutor.Application
EnglishLiveTutor.Api  -->  EnglishLiveTutor.Infrastructure (Composition Root)

EnglishLiveTutor.Infrastructure  -->  EnglishLiveTutor.Application
EnglishLiveTutor.Infrastructure  -->  EnglishLiveTutor.Domain

EnglishLiveTutor.Application  -->  EnglishLiveTutor.Domain

EnglishLiveTutor.Domain  -->  (No dependencies on other application projects)
```

---

## 2. Project Structure

```text
backend/
  EnglishLiveTutor.sln
  docs/
    architecture.md
  src/
    EnglishLiveTutor.Api/
      EnglishLiveTutor.Api.csproj
      Program.cs
      appsettings.json
      appsettings.Development.json
      Properties/
        launchSettings.json
      Endpoints/
        ConversationEndpoints.cs
        DailyLearningEndpoints.cs
        HealthEndpoints.cs
        PronunciationEndpoints.cs
        VocabularyEndpoints.cs
      Hubs/
        TranscribeLiveHub.cs
        TranslateLiveHub.cs
        TutorConversationHub.cs
      Realtime/
        SignalRLiveSessionRealtimeNotifier.cs
      Services/
        HttpContextCurrentUser.cs
    EnglishLiveTutor.Application/
      EnglishLiveTutor.Application.csproj
      DependencyInjection.cs
      Common/
        Interfaces/
          IAiServices.cs
          IAppDbContext.cs
          ICqrs.cs
          ICurrentUser.cs
          ILiveSessionRealtimeNotifier.cs
        Models/
          LiveSessionDtos.cs
          VocabularyDtos.cs
      Features/
        Conversations/
          Commands/
            CreateConversation/
            UpdateConversationSettings/
            DeleteConversation/
          Queries/
            ListConversations/
            GetConversationHistory/
        Learning/
          Commands/
            InitializeDailyVocabularyPlan/
            SubmitVocabularyReview/
          Queries/
            GetDailyVocabularyPlan/
            GetLearningProgress/
            GetLearningHistory/
        Pronunciation/
          Commands/
            AssessPronunciation/
            UpdateScoringRubric/
          Queries/
            GetActiveScoringRubric/
            GetPronunciationHistory/
            GetPronunciationModels/
        Vocabulary/
          VocabularyProjectionHelper.cs
          Commands/
            GenerateVocabulary/
          Queries/
            GetVocabularyFlashcard/
            GetVocabularyGenerationModels/
    EnglishLiveTutor.Domain/
      EnglishLiveTutor.Domain.csproj
      Common/
        Abstractions/
          IAuditable.cs
          ISoftDeletable.cs
        Primitives/
          Entity.cs
          AggregateRoot.cs
      AiCatalog/
        AiModelConfigEntity.cs
        Repositories/
          IAiModelCatalogRepository.cs
      Conversations/
        ConversationEntity.cs
        Entities/
          ChatMessageEntity.cs
          LanguageCorrectionEntity.cs
        Repositories/
          IConversationRepository.cs
      Learning/
        DailyVocabularyPlanEntity.cs
        UserVocabularyProgressEntity.cs
        VocabularyReviewAttemptEntity.cs
        Entities/
          DailyVocabularyItemEntity.cs
        Repositories/
          IDailyVocabularyPlanRepository.cs
          IUserVocabularyProgressRepository.cs
      Pronunciation/
        AudioRecordingEntity.cs
        PronunciationAssessmentEntity.cs
        ScoringRubricEntity.cs
        Entities/
          PronunciationCriterionScoreEntity.cs
          ScoringRubricCriterionEntity.cs
        Repositories/
          IPronunciationAssessmentRepository.cs
          IScoringRubricRepository.cs
      Vocabulary/
        VocabularyEntity.cs
        VocabularyGenerationAuditEntity.cs
        Enums/
          VocabularySourceType.cs
        Entities/
          VocabularyCollocationEntity.cs
          VocabularyExampleEntity.cs
          VocabularyGrammarPatternEntity.cs
          VocabularyPronunciationEntity.cs
          VocabularyRelationEntity.cs
          VocabularySenseEntity.cs
          VocabularyWordFamilyEntity.cs
        Repositories/
          IVocabularyRepository.cs
    EnglishLiveTutor.Infrastructure/
      EnglishLiveTutor.Infrastructure.csproj
      DependencyInjection.cs
      Ai/
        CefrPromptBuilder.cs
        FastTextTranslatorFallback.cs
        GeminiPronunciationAiAssessor.cs
        GeminiVocabularyAiGenerator.cs
        GrammarCorrectionAnalyzerService.cs
      Migrations/
        20261014120000_InitialCreateLingoMintSqlServer.cs
        20261014155905_AddSupportedModes.cs
        20261014165240_AddFlashCard.cs
        20261015010000_AddAuditingAndSoftDelete.cs
        AppDbContextModelSnapshot.cs
      Persistence/
        AppDbContext.cs
        DatabaseSeeder.cs
        Configurations/
          AiModelConfigConfiguration.cs
          ConversationConfigurations.cs
          EntityConfigurationExtensions.cs
          LearningConfigurations.cs
          PronunciationConfigurations.cs
          VocabularyConfigurations.cs
        Interceptors/
          AuditingSaveChangesInterceptor.cs
      Realtime/
        GeminiLiveConnectionManager.cs
        GeminiLiveSessionBridge.cs
        PcmAudioResampler.cs
        SmartTextDeduplicator.cs
      Repositories/
        AiModelCatalogRepository.cs
        ConversationRepository.cs
        DailyVocabularyPlanRepository.cs
        PronunciationAssessmentRepository.cs
        ScoringRubricRepository.cs
        UserVocabularyProgressRepository.cs
        VocabularyRepository.cs
  tests/
    EnglishLiveTutor.Domain.Tests/
      EnglishLiveTutor.Domain.Tests.csproj
      DomainLogicTests.cs
      PersistenceAndAuditingTests.cs
```

---

## 3. DDD Base Classes & Interfaces (`EnglishLiveTutor.Domain/Common`)

### 3.1 `IAuditable`
Implemented by `Entity<TId>` and inherited by all entities and aggregate roots:
- `DateTimeOffset CreatedAtUtc { get; }`
- `string? CreatedBy { get; }`
- `DateTimeOffset? UpdatedAtUtc { get; }`
- `string? UpdatedBy { get; }`

### 3.2 `ISoftDeletable`
Implemented by `AggregateRoot<TId>`:
- `bool IsDeleted { get; }`
- `DateTimeOffset? DeletedAtUtc { get; }`
- `string? DeletedBy { get; }`

### 3.3 `Entity<TId>` (`where TId : notnull`)
- Generic base class implementing `IAuditable` and `IEquatable<Entity<TId>>`.
- Encapsulates `Id` and audit properties with `protected set;` mutators and EF Core-compatible parameterless constructors.
- Eliminates duplicate audit property definitions across individual domain entities.

### 3.4 `AggregateRoot<TId>` (`where TId : notnull`)
- Inherits `Entity<TId>` and implements `ISoftDeletable`.
- Provides idempotent `SoftDelete(DateTimeOffset deletedAtUtc, string? deletedBy = null)` and `Restore()` methods.
- Repeated calls to `SoftDelete` on an already deleted aggregate preserve the original `DeletedAtUtc` and `DeletedBy` values.

---

## 4. Aggregate-Centric Domain Organization

1. **Vocabulary Aggregate (`Domain/Vocabulary`)**
   - Root: `VocabularyEntity : AggregateRoot<Guid>`
   - Child entities (`Entity<Guid>`): `VocabularySenseEntity` (owning `VocabularyRelationEntity`, `VocabularyCollocationEntity`, `VocabularyExampleEntity`, `VocabularyGrammarPatternEntity`), `VocabularyPronunciationEntity`, and `VocabularyWordFamilyEntity`.
   - Encapsulates child mutations via `AddSense`, `AddPronunciation`, `AddWordFamily`, `UpdateDefinition`, `Activate`, `Deactivate`, and `NormalizeWord`.
   - Preserves `IsActive != !IsDeleted` (`IsActive` controls catalog availability; `IsDeleted` represents logical deletion).
   - `VocabularyGenerationAuditEntity : AggregateRoot<Guid>` is persisted as an append-only audit log.
2. **AI Catalog Aggregate (`Domain/AiCatalog`)**
   - Root: `AiModelConfigEntity : AggregateRoot<Guid>` (decoupled from Vocabulary into its own domain area).
3. **Conversations Aggregate (`Domain/Conversations`)**
   - Root: `ConversationEntity : AggregateRoot<string>`
   - Child entities (`Entity<string>`): `ChatMessageEntity`, `LanguageCorrectionEntity`.
4. **Learning Aggregate (`Domain/Learning`)**
   - Roots: `DailyVocabularyPlanEntity : AggregateRoot<Guid>` (owning `DailyVocabularyItemEntity : Entity<Guid>`), `UserVocabularyProgressEntity : AggregateRoot<Guid>`, and append-only `VocabularyReviewAttemptEntity : AggregateRoot<Guid>`.
5. **Pronunciation Aggregate (`Domain/Pronunciation`)**
   - Roots: `ScoringRubricEntity : AggregateRoot<Guid>` (owning `ScoringRubricCriterionEntity : Entity<Guid>`), `PronunciationAssessmentEntity : AggregateRoot<Guid>` (owning `PronunciationCriterionScoreEntity : Entity<Guid>`), and `AudioRecordingEntity : AggregateRoot<Guid>`.

---

## 5. Persistence, Automatic Auditing, and Soft Delete (`EnglishLiveTutor.Infrastructure`)

- **Single Canonical `AppDbContext`**: Located at `Infrastructure/Persistence/AppDbContext.cs`, with entity mappings modularized into `IEntityTypeConfiguration<T>` classes under `Infrastructure/Persistence/Configurations/`.
- **Backward-Compatible Column Mapping**: Existing `CreatedAt` and `UpdatedAt` columns on `Vocabularies`, `VocabularyGenerationAudits`, `Conversations`, `Messages`, `Corrections`, `DailyVocabularyPlans`, `ScoringRubrics`, `AudioRecordings`, and `PronunciationAssessments` are preserved via `.HasColumnName("CreatedAt")` and `.HasColumnName("UpdatedAt")`.
- **`AuditingSaveChangesInterceptor`**:
  - Uses `TimeProvider` and `ICurrentUser` (`HttpContextCurrentUser` in API requests, falling back to `SystemCurrentUser` `"system"` during seeding and background jobs).
  - Sets `CreatedAtUtc` and `CreatedBy` when `EntityState.Added`.
  - Sets `UpdatedAtUtc` and `UpdatedBy` when `EntityState.Modified` while protecting `CreatedAtUtc` and `CreatedBy` from modification.
  - Intercepts any `EntityState.Deleted` on `ISoftDeletable` Aggregate Roots and converts it into a logical `SoftDelete` while keeping child entities intact (`EntityState.Unchanged`).
- **Global Soft-Delete Query Filters**:
  - Every `AggregateRoot<TId>` is configured with `.HasQueryFilter(e => !e.IsDeleted)`.
  - `AppDbContext.QueryIncludingDeleted<TEntity>()` provides explicit access using `.IgnoreQueryFilters()` when required.

---

## 6. CQRS Feature-Based Application (`EnglishLiveTutor.Application`)

Each feature (`Conversations`, `Learning`, `Pronunciation`, `Vocabulary`) is organized into `Commands/<UseCase>/` and `Queries/<UseCase>/`:
- **Commands** mutate state through Domain Aggregate Root methods and Aggregate Repositories.
- **Queries** use `AsNoTracking()` projections via `IAppDbContextFactory` to optimize read performance without unnecessary aggregate materialization.
- **API Relocation**: `EnglishLiveTutor.Api` resides under `src/EnglishLiveTutor.Api/`, keeping HTTP endpoints and SignalR hubs thin and focused on transport, routing, and serialization while preserving all existing frontend contracts.

`VocabularyEndpoints`, `PronunciationEndpoints` (including `/api/ai-models`), and `PromptEndpoints` dispatch every use case through `ICommandHandler` or `IQueryHandler`. Their HTTP layer binds requests, enforces permissions, and formats results; it does not create EF contexts or call AI services directly. List queries execute filtering, counting, ordering, and paging in Application through `IAppDbContextFactory`. Commands persist tracked aggregates through specialized repositories, including `ISpeechSpeedRepository` for speech-speed configuration. Vocabulary and AI-model updates modify existing rows, preserving aggregate IDs and creation audit; vocabulary updates also preserve the primary sense ID and other senses.

Speech-speed lists read the database only. Default speech speeds belong to database seeding; empty filters/pages do not synthesize fallback entries inside the GET endpoint.

### Vocabulary generation timeout

`GeminiVocabularyAiGenerator` receives a typed `HttpClient` registered in Infrastructure. Configure its timeout in the API configuration:

```json
{
  "Ai": {
    "VocabularyGeneration": {
      "TimeoutSeconds": 300
    }
  }
}
```

The default is 300 seconds; allowed values are 1–3600 seconds. Set `Ai__VocabularyGeneration__TimeoutSeconds=600` to allow ten minutes through an environment variable. This setting affects vocabulary generation only. The request cancellation token still propagates to the Google HTTP call and may cancel it earlier. Changing the setting requires restarting the API. The HTTP timeout is separate from database command timeouts and from any browser/proxy request limits.

---

## 7. Identity, Prompts (DB-Backed `CefrPromptBuilder`), and Teacher Voices Catalog

1. **Identity & Access Management Aggregate (`Domain/Identity`)**:
   - **Aggregate Roots**: `UserEntity : AggregateRoot<Guid>`, `RoleEntity : AggregateRoot<Guid>`, `PermissionEntity : AggregateRoot<Guid>`.
   - **Child Entities**: `UserRoleEntity : Entity<Guid>`, `RolePermissionEntity : Entity<Guid>`, `RefreshTokenEntity : Entity<Guid>` (supports token issuing, rotation with `ReplacedByToken`, and revocation).
   - **Security Services**: `Pbkdf2PasswordHasher` (`PBKDF2-SHA256`) and `HmacJwtTokenService` (`HS256` JWT + Refresh Token generator).
   - **Endpoints**: `/api/auth/register`, `/api/auth/login`, `/api/auth/refresh`, `/api/auth/revoke`, `/api/users`, `/api/roles`, `/api/permissions`.

2. **Prompts & Coaching Modes Aggregate (`Domain/Prompts` & `Infrastructure/Ai/CefrPromptBuilder.cs`)**:
   - **Aggregate Roots**: `CefrLevelPromptEntity : AggregateRoot<Guid>` (6 CEFR levels `A1`–`C2`) and `ConversationModePromptEntity : AggregateRoot<Guid>` (19 Interview, Coaching, and Workplace modes with `{cefrLevel}` template interpolation).
   - **`ICefrPromptBuilder` / `CefrPromptBuilder`**: Loads active CEFR level guidance and session mode directives from SQL Server (`CefrLevelPrompts` and `ConversationModePrompts`) with deterministic fallback defaults.
   - **Endpoints**: `GET/PUT /api/prompts/cefr-levels`, `GET/PUT /api/prompts/modes`, `POST /api/prompts/preview`.

3. **Teacher Voices Catalog Aggregate (`Domain/Voices`)**:
   - **Aggregate Root**: `TeacherVoiceEntity : AggregateRoot<string>`.
   - **Seeded Catalog**: Seeds all **30 Google Prebuilt Voices** (`Aoede`, `Puck`, `Kore`, `Charon`, `Fenrir`, `Leda`, `Zephyr`, `Orus`, `Callirrhoe`, `Autonoe`, `Enceladus`, `Iapetus`, `Umbriel`, `Algieba`, `Despina`, `Erinome`, `Algenib`, `Rasalgethi`, `Laomedeia`, `Achernar`, `Alnilam`, `Schedar`, `Gacrux`, `Pulcherrima`, `Achird`, `Zubenelgenubi`, `Vindemiatrix`, `Sadachbia`, `Sadaltager`, `Sulafat`), marking the 6 primary teacher voices as `IsFeatured = true` with Vietnamese `Label` and `Style` metadata.
   - **Endpoints**: `GET /api/voices`, `GET /api/voices/{id}`, `PUT /api/voices/{id}`.
