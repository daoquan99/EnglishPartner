---
trigger: manual
---

# Common: Event Patterns

> **Scope:** Domain Events, Integration Events, Outbox/Inbox pattern, Event Bus
> @mention this file when working on: cross-module communication, domain events, integration events, outbox processor

---

## 1. Domain Event

### Định nghĩa:
```csharp
// Trong Domain layer — đặt trong entity's Events/ folder
public sealed record VocabularyCreatedDomainEvent(Guid VocabularyId) : DomainEvent;
public sealed record UserRegisteredDomainEvent(Guid UserId, string Email) : DomainEvent;
```

### Raise trong Entity:
```csharp
// Entity.Create() hoặc business method:
entity.RaiseDomainEvent(new VocabularyCreatedDomainEvent(entity.Id));
```

### Handler (Application layer):
```csharp
internal sealed class VocabularyCreatedDomainEventHandler(
    IEventBus eventBus,
    ICacheService cache)
    : INotificationHandler<DomainEventNotification<VocabularyCreatedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<VocabularyCreatedDomainEvent> notification, CancellationToken ct)
    {
        var domainEvent = notification.DomainEvent;

        // 1. Publish integration event (nếu cần cross-module):
        await eventBus.PublishAsync(
            new VocabularyCreatedIntegrationEvent(domainEvent.VocabularyId), ct);

        // 2. Cache invalidation (preferred over inline in handlers):
        await cache.RemoveByPrefixAsync($"curriculum:vocabularies:en:", ct);
    }
}
```

**Naming:** `{Entity}{PastTense}DomainEvent` — e.g., `VocabularyCreatedDomainEvent`, `UserDeactivatedDomainEvent`

**Dispatch:** Tự động bởi `ConvertDomainEventsToOutboxMessagesInterceptor` sau `SaveChanges`. KHÔNG dispatch thủ công.

---

## 2. Integration Event

### Định nghĩa (trong `.Contracts` project):
```csharp
// EnglishPartner.Modules.Curriculum.Contracts/IntegrationEvents/
public sealed record VocabularyCreatedIntegrationEvent(Guid VocabularyId) : IntegrationEvent;
```

**Quy tắc:**
- Chỉ chứa primitive types / serializable data — **KHÔNG** có Domain objects
- Naming: `{Entity}{PastTense}IntegrationEvent`
- Public — được tham chiếu bởi module khác

---

## 3. Publish Flow (Outbox Pattern)

```
Entity.RaiseDomainEvent(event)
    ↓
[SaveChanges]
    ↓
ConvertDomainEventsToOutboxMessagesInterceptor
    → Serialize event → INSERT OutboxMessage (same DB transaction)
    ↓
Quartz: ProcessOutboxMessagesJob (every 10s)
    → SELECT unprocessed OutboxMessages
    → Deserialize → publisher.Publish() via MediatR
    → DomainEvent Handler runs
    → IEventBus.PublishAsync() → InMemoryEventBus resolves handlers from DI
    ↓
IIntegrationEventHandler<T> runs in consumer module (same process)
```

**OutboxMessage table:** Mỗi module có bảng riêng trong schema của mình (curriculum.outbox_messages, identity.outbox_messages, ...).

---

## 4. Consume Flow (Inbox / Idempotency)

```csharp
internal sealed class UserRegisteredIntegrationEventHandler
    : IdempotentIntegrationEventHandler<UserRegisteredIntegrationEvent, LearningDbContext>
{
    public UserRegisteredIntegrationEventHandler(
        LearningDbContext dbContext,
        ISender sender,
        TimeProvider timeProvider,
        ILogger<UserRegisteredIntegrationEventHandler> logger)
        : base(dbContext, timeProvider, logger) { }

    protected override async Task HandleInternalAsync(
        UserRegisteredIntegrationEvent notification, CancellationToken ct)
    {
        // Business logic here
        // SaveChanges handled by base class (commits business data + InboxMessage atomically)
    }
}
```

`IdempotentIntegrationEventHandler<TEvent, TDbContext>` base class tự động:
1. Check `InboxMessage` table — nếu đã xử lý → skip (dedup)
2. Gọi `HandleInternalAsync()` — derived class implement business logic
3. Ghi vào `InboxMessage` + `SaveChanges` sau khi xử lý thành công

---

## 5. IEventBus & InMemoryEventBus

```csharp
public interface IEventBus
{
    Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default)
        where T : IntegrationEvent;
}
```

**Implementation:** `InMemoryEventBus` — registered as **Singleton**, uses `IServiceScopeFactory` to create a scope and resolve scoped `IIntegrationEventHandler<T>` from DI.

---

## 6. DI Registration — BẮT BUỘC

> [!CAUTION]
> **Mỗi `IIntegrationEventHandler<T>` PHẢI được đăng ký tường minh (explicit) trong `DependencyInjection.cs` của module Infrastructure.** `InMemoryEventBus` resolve handler qua `IServiceProvider.GetServices()` — KHÔNG có auto-discovery/assembly scanning.

### Template đăng ký:
```csharp
// Trong Module.Infrastructure/DependencyInjection.cs:
services.AddScoped<IIntegrationEventHandler<SomeIntegrationEvent>,
    IntegrationEventHandlers.SomeIntegrationEventHandler>();
```

### Checklist khi tạo Integration Event mới:
1. ✅ Tạo event record trong `Module.Contracts/IntegrationEvents/`
2. ✅ Tạo handler trong consumer `Module.Infrastructure/IntegrationEventHandlers/`
3. ✅ **Đăng ký handler trong consumer `Module.Infrastructure/DependencyInjection.cs`**
4. ✅ Thêm vào bảng Known Events (section 7 bên dưới)

> Nếu quên bước 3, `InMemoryEventBus` sẽ log `Found 0 handlers` và event sẽ bị bỏ qua (silently lost).

---

## 7. Known Integration Events

### Identity Module (`Identity.Contracts`)

| Event | Parameters | Publisher | Consumer(s) | DI Registered |
|---|---|---|---|---|
| `UserRegisteredIntegrationEvent` | `UserId, Email, DisplayName, RegisteredAt` | `UserRegisteredDomainEventHandler` | Learning ✅, Notifications ✅ | ✅ |
| `OnboardingCompletedIntegrationEvent` | `UserId, ProficiencyLevel, LearningGoal, TargetLanguage, DailyGoalMinutes, MorningReminderHour?, EveningReminderHour?, UtcOffsetHours` | `OnboardingCompletedDomainEventHandler` | Learning ✅ | ✅ |
| `UserProfileUpdatedIntegrationEvent` | `UserId, DailyGoalMinutes, MorningReminderHour?, EveningReminderHour?, UtcOffsetHours` | `UserProfileUpdatedDomainEventHandler` | Learning ✅ | ✅ |

### Learning Module (`Learning.Contracts`)

| Event | Parameters | Publisher | Consumer(s) | DI Registered |
|---|---|---|---|---|
| `StreakUpdatedIntegrationEvent` | `UserId, CurrentStreak, LongestStreak` | `StudySessionCompletedDomainEventHandler` | Notifications ✅ | ✅ |
| `LevelUpIntegrationEvent` | `UserId, PreviousLevel, NewLevel, TotalXp` | `LevelUpDomainEventHandler` | Notifications ✅ | ✅ |
| `DailyGoalReminderIntegrationEvent` | `UserId, Email, ReminderType, MinutesStudied, DailyGoalMinutes` | `DailyGoalReminderJob` | Notifications ✅ | ✅ |

### Curriculum Module (`Curriculum.Contracts`)

| Event | Parameters | Publisher | Consumer(s) | DI Registered |
|---|---|---|---|---|
| `ExerciseCompletedIntegrationEvent` | `UserId, ExerciseId, XpEarned` | `SubmitExerciseCommandHandler` | Learning ✅ | ✅ |
| `VocabularyCreatedIntegrationEvent` | `VocabularyId, Word, LanguageCode` | — | ⏳ No consumer yet | — |
| `ExerciseCreatedIntegrationEvent` | `ExerciseId, Title, LanguageCode` | — | ⏳ No consumer yet | — |
| `ConversationCreatedIntegrationEvent` | `ConversationId, TopicName, LanguageCode` | — | ⏳ No consumer yet | — |

### Known Domain Events

| Module | Event | Handler Location |
|---|---|---|
| Identity | `UserRegisteredDomainEvent(UserId, Email, DisplayName)` | Identity.Infrastructure → publishes `UserRegisteredIntegrationEvent` |
| Identity | `OnboardingCompletedDomainEvent(...)` | Identity.Infrastructure → publishes `OnboardingCompletedIntegrationEvent` |
| Identity | `UserProfileUpdatedDomainEvent(...)` | Identity.Infrastructure → publishes `UserProfileUpdatedIntegrationEvent` |
| Identity | `StreakUpdatedDomainEvent(UserId, CurrentStreak, LongestStreak)` | — (not currently handled) |
| Learning | `LevelUpDomainEvent(UserId, PreviousLevel, NewLevel, TotalXp)` | Learning.Application → publishes `LevelUpIntegrationEvent` |
| Learning | `StudySessionCompletedDomainEvent(SessionId, UserId, TotalItems)` | Learning.Application → publishes `StreakUpdatedIntegrationEvent` |
| Curriculum | `VocabularyCreatedDomainEvent(VocabularyId)` | — (not currently handled) |
| Curriculum | `TopicCreatedDomainEvent(TopicId)` | — (not currently handled) |
| Curriculum | `ExerciseCreatedDomainEvent(ExerciseId)` | — (not currently handled) |

> ⚠️ **Ownership rule:** `StreakUpdatedIntegrationEvent` belongs to `Learning.Contracts` — streak is a *learning activity* metric, NOT an identity concern. Do NOT move it to Identity.Contracts.

📍 **Xem bảng đầy đủ** (Module Interfaces, event flow diagram) tại [`common/cross_module.md`](./cross_module.md).

---

## 8. Domain Event vs Integration Event — When to Use

| Scenario | Use |
|---|---|
| Side effect trong cùng module (cache invalidation, audit log) | Domain Event Handler |
| Thông báo sang module khác (cross-boundary) | Integration Event via IEventBus |
| Realtime notification về UI | Integration Event → SignalR in Notifications module |
| Ghi audit log trong cùng transaction | Domain Event Handler |
