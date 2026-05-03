---
trigger: manual
---

# Common: Cross-Module Communication Map

> **Scope:** Toàn cảnh giao tiếp giữa các modules — Integration Events, Module Interfaces, Event Flow
> @mention this file when working on: cross-module feature, integration event, module interface, event handler

---

## 1. Event Flow Diagram

```
 ┌──────────┐          ┌──────────────┐          ┌──────────────┐
 │ Identity │─publish──▶│   Learning   │─publish──▶│Notifications │
 │          │          │              │          │              │
 │ UserReg  │──────────▶│ CreateProfile│          │ Welcome      │
 │ Onboard  │──────────▶│ SetLevel     │          │              │
 │ ProfUpd  │──────────▶│ SyncPrefs    │          │              │
 │          │          │              │          │              │
 │          │          │ SessionDone  │──────────▶│ StreakPush   │
 │          │          │ LevelUp      │──────────▶│ LevelUpPush  │
 │          │          │ DailyRemind  │──────────▶│ ReminderPush │
 └──────────┘          └──────────────┘          └──────────────┘
       │                      ▲
       │                      │ sync query
       │               ┌──────┴───────┐
       │               │     AI       │
       │               │              │
       │               │ EvaluateAsync│◀──── Learning (SubmitAnswer)
       │               │ GenerateAsync│◀──── Curriculum (AI Generate)
       │               │ GetLevel     │───── Learning.Contracts (ILearningProfileQueryService)
       │               │ GetContext   │───── Curriculum.Contracts (ICurriculumQueryService)
       │               └──────────────┘
```

---

## 2. Integration Events — Bảng tổng hợp

### Identity → Others

| Event | Parameters | Consumer(s) | Action |
|---|---|---|---|
| `UserRegisteredIntegrationEvent` | UserId, Email, DisplayName, RegisteredAt | **Learning**: Tạo `UserLearningProfile`. **Notifications**: Tạo welcome notification | 
| `OnboardingCompletedIntegrationEvent` | UserId, ProficiencyLevel, LearningGoal, TargetLanguage, DailyGoalMinutes, MorningReminderHour?, EveningReminderHour?, UtcOffsetHours | **Learning**: Set `CurrentLevel` + `StudyPreferences` lần đầu |
| `UserProfileUpdatedIntegrationEvent` | UserId, DailyGoalMinutes, MorningReminderHour?, EveningReminderHour?, UtcOffsetHours | **Learning**: Sync `StudyPreferences` |

**Contracts project:** `Identity.Contracts/IntegrationEvents/IdentityIntegrationEvents.cs`

### Learning → Others

| Event | Parameters | Consumer(s) | Action |
|---|---|---|---|
| `StreakUpdatedIntegrationEvent` | UserId, CurrentStreak, LongestStreak | **Notifications**: Streak milestone push |
| `LevelUpIntegrationEvent` | UserId, PreviousLevel, NewLevel, TotalXp | **Notifications**: Level-up congratulation push |
| `DailyGoalReminderIntegrationEvent` | UserId, ReminderType (Morning/Evening), MinutesStudied, DailyGoalMinutes | **Notifications**: Study reminder push |

**Contracts project:** `Learning.Contracts/IntegrationEvents/LearningIntegrationEvents.cs`

### Curriculum → Others (⚠️ Pending / Bug)

| Event | Parameters | Status | Note |
|---|---|---|---|
| `VocabularyCreatedIntegrationEvent` | VocabularyId, Word, LanguageCode | 🔴 **BUG** | Kế thừa `DomainEvent` thay vì `IntegrationEvent` |
| `ExerciseCreatedIntegrationEvent` | ExerciseId, Title, LanguageCode | 🔴 **BUG** | Kế thừa `DomainEvent` thay vì `IntegrationEvent` |
| `ConversationCreatedIntegrationEvent` | ConversationId, TopicName, LanguageCode | 🔴 **BUG** | Kế thừa `DomainEvent` thay vì `IntegrationEvent` |

> ⚠️ **BUG-4 (Curriculum.Contracts):** 3 events trong `CurriculumIntegrationEvents.cs` kế thừa `: DomainEvent` thay vì `: IntegrationEvent`. Chúng **KHÔNG THỂ** được publish qua `IEventBus` vì `IEventBus.PublishAsync<T>` yêu cầu `T : IntegrationEvent`. Cần sửa base class trước khi implement consumer.
>
> **Hành động:** Đổi `DomainEvent` → `IntegrationEvent` + thêm `using EnglishPartner.Messaging;`.

### AI → Others

Không có. AI module là **pure consumer** — chỉ nhận request qua Module Interfaces, không publish events.

### Notifications → Others

Không có. Notifications là **terminal consumer** — chỉ nhận events, không publish.

---

## 3. Module Interfaces (Sync — Cross-Module Query/Command)

Module Interfaces cho phép gọi trực tiếp (sync) giữa modules mà không cần events. Defined trong `.Contracts`, implemented trong source module's `.Infrastructure`.

### Curriculum.Contracts → AI consumes

| Interface | Method | Returns | Used by |
|---|---|---|---|
| `ICurriculumQueryService` | `GetItemContextAsync(itemId, itemType, ct)` | `CurriculumItemContextDto?` | AI: Lấy context cho Evaluate (text + audio) |
| — | — | `{ ItemType, Question, CorrectAnswer, Hint?, LanguageCode }` | — |

**CurriculumItemType enum (Contracts):**

| Value | Name |
|---|---|
| 1 | Vocabulary |
| 2 | Grammar |
| 3 | Exercise |
| 4 | ExampleSentence |
| 5 | Conversation |
| 6 | Topic |
| 7 | ConversationLine |

> ⚠️ **Khác với Learning.ItemType** (bắt đầu từ 0). Xem mapping table trong `ai_evaluation.md`.

### AI.Contracts → Learning + Curriculum consume

| Interface | Method | Returns | Used by |
|---|---|---|---|
| `IAiEvaluationService` | `EvaluateTextAnswerAsync(question, correctAnswer, userAnswer, ct)` | `AiTextEvaluationResult?` | **Learning**: SubmitAnswer handler → Score (0-100), IsCorrect, Explanation |
| — | `EvaluatePronunciationAsync(targetText, audioStream, targetLanguage, ct)` | `AiAudioEvaluationResult?` | **Learning**: SubmitAudioAnswer + EvaluateConversationLine → Score (0-100), Transcription, Feedback |
| — | `GenerateTtsAsync(text, targetLanguage, voiceOptions?, ct)` | `Stream?` | **Curriculum**: Background job tạo audio cho ConversationLine |
| `ICurriculumContentGenerationService` | `GenerateVocabularyAsync(...)` | `List<GeneratedVocabularyDto>` | **Curriculum**: AI Generate Vocabulary command |
| — | `GenerateGrammarAsync(...)` | `List<GeneratedGrammarDto>` | **Curriculum**: AI Generate Grammar command |
| — | `GenerateConversationAsync(...)` | `List<GeneratedConversationContentDto>` | **Curriculum**: AI Generate Conversation command |
| — | `GenerateExerciseAsync(...)` | `List<GeneratedExerciseDto>` | **Curriculum**: AI Generate Exercise command |
| — | `GenerateExampleSentencesAsync(...)` | `List<GeneratedExampleSentenceDto>` | **Curriculum**: AI Generate ExampleSentences command |

### Learning.Contracts → AI consumes

| Interface | Method | Returns | Used by |
|---|---|---|---|
| `ILearningProfileQueryService` | `GetUserLevelAsync(userId, ct)` | `UserLearningLevelDto?` | **AI**: Chat StartSession — lấy CEFR level để build System Prompt |
| — | — | `{ CurrentLevel, TargetLanguage, TotalXp, CurrentStreak }` | — |

---

## 4. Dependency Graph (Contracts References)

```
Identity.Contracts ──referenced by──▶ Learning.Infrastructure
                                      Notifications.Infrastructure

Learning.Contracts ──referenced by──▶ AI.Infrastructure (ILearningProfileQueryService)
                                      Notifications.Infrastructure

Curriculum.Contracts ──referenced by──▶ AI.Infrastructure (ICurriculumQueryService)
                                        AI.Api (CurriculumItemType enum)

AI.Contracts ──referenced by──▶ Learning.Infrastructure (IAiEvaluationService)
                                 Curriculum.Infrastructure (ICurriculumContentGenerationService)

Notifications.Contracts ──referenced by──▶ (empty — no consumers)
```

> **Rule:** Contracts project CHỈCHỨA: Integration Events + Module Interface definitions + DTOs.
> **KHÔNG** chứa: Domain entities, Application logic, Infrastructure.

---

## 5. Checklist — Thêm Integration Event mới

1. [ ] Tạo record trong `{Module}.Contracts/IntegrationEvents/` kế thừa `: IntegrationEvent`
2. [ ] Tạo Domain Event tương ứng trong `{Module}.Domain/{Entity}/Events/`
3. [ ] Raise Domain Event trong entity method: `RaiseDomainEvent(new XxxDomainEvent(...))`
4. [ ] Tạo Domain Event Handler trong `{Module}.Application/` — convert sang Integration Event + publish qua `IEventBus`
5. [ ] Tạo Integration Event Handler trong consumer module: kế thừa `IdempotentIntegrationEventHandler<TEvent>`
6. [ ] Update `common/cross_module.md` bảng tổng hợp
7. [ ] Update module index file (e.g. `learning.md`) phần Domain Events / Integration Events

---

## 6. Checklist — Thêm Module Interface mới

1. [ ] Tạo interface + DTOs trong `{Provider}.Contracts/Services/`
2. [ ] Implement interface trong `{Provider}.Infrastructure/Services/`
3. [ ] Register DI trong `{Provider}.Infrastructure/DependencyInjection.cs`
4. [ ] Consumer module chỉ tham chiếu `{Provider}.Contracts` project
5. [ ] Update `common/cross_module.md` bảng Module Interfaces
