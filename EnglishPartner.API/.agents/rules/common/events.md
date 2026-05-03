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
// EnglishPartner.Modules.Curriculum.Contracts/Events/
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
    → IEventBus.PublishAsync() → saves IntegrationEvent to OutboxMessage
    ↓
Quartz: ProcessOutboxMessagesJob again
    → IntegrationEvent Handler runs in consumer module
```

**OutboxMessage table:** Mỗi module có bảng riêng trong schema của mình (curriculum.outbox_messages, identity.outbox_messages, ...).

---

## 4. Consume Flow (Inbox / Idempotency)

```csharp
internal sealed class UserRegisteredIntegrationEventHandler(
    ILearningUnitOfWork uow,
    LearningDbContext db)
    : IdempotentIntegrationEventHandler<UserRegisteredIntegrationEvent>
{
    protected override async Task HandleCore(
        UserRegisteredIntegrationEvent integrationEvent, CancellationToken ct)
    {
        // Create learning profile for new user
        var profile = LearningProfile.Create(integrationEvent.UserId);
        db.LearningProfiles.Add(profile);
        // SaveChanges handled by UoW behavior
    }
}
```

`IdempotentIntegrationEventHandler` base class tự động:
1. Check `InboxMessage` table — nếu đã xử lý → skip (dedup)
2. Ghi vào `InboxMessage` sau khi xử lý thành công

---

## 5. IEventBus

```csharp
public interface IEventBus
{
    Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default)
        where T : IntegrationEvent;
}
```

**Implementation:** `InMemoryEventBus` — at-least-once delivery (vì Outbox retry).

---

## 6. Known Integration Events

| Source Module | Event | Consumer Modules |
|---|---|---|
| Identity | `UserRegisteredIntegrationEvent` | Learning (create profile), Notifications (welcome notification) |
| Identity | `OnboardingCompletedIntegrationEvent` | Learning (set initial CEFR level + StudyPreferences) |
| Identity | `UserProfileUpdatedIntegrationEvent` | Learning (sync StudyPreferences: dailyGoalMinutes, reminderHours, utcOffsetHours) |
| Learning | `StreakUpdatedIntegrationEvent` | Notifications (streak milestone push) |
| Learning | `LevelUpIntegrationEvent` | Notifications (level-up congratulation push) |
| Learning | `DailyGoalReminderIntegrationEvent` | Notifications (morning/evening study reminder push) |
| Curriculum | `VocabularyCreatedIntegrationEvent` | ⏳ Pending (chưa có consumer) |
| Curriculum | `ExerciseCreatedIntegrationEvent` | ⏳ Pending (chưa có consumer) |
| Curriculum | `ConversationCreatedIntegrationEvent` | ⏳ Pending (chưa có consumer) |

> ⚠️ **Ownership rule:** `StreakUpdatedIntegrationEvent` belongs to `Learning.Contracts` — streak is a *learning activity* metric, NOT an identity concern. Do NOT move it to Identity.Contracts.
>
> ⚠️ **Domain Event khác Integration Event:** Identity module cũng có `StreakUpdatedDomainEvent` (in-process) và `UserProfileUpdatedDomainEvent` (in-process). Các DomainEvent này được handler trong Identity.Application convert thành IntegrationEvent tương ứng rồi publish qua `IEventBus`.
>
> 🔴 **BUG-4:** 3 Curriculum events (`VocabularyCreated`, `ExerciseCreated`, `ConversationCreated`) trong Contracts kế thừa `: DomainEvent` thay vì `: IntegrationEvent`. Cần sửa base class trước khi implement consumer. Chi tiết tại `common/cross_module.md`.

📍 **Xem bảng đầy đủ** (parameters, Module Interfaces, event flow diagram) tại [`common/cross_module.md`](./cross_module.md).

---

## 7. Domain Event vs Integration Event — When to Use

| Scenario | Use |
|---|---|
| Side effect trong cùng module (cache invalidation, audit log) | Domain Event Handler |
| Thông báo sang module khác (cross-boundary) | Integration Event via Outbox |
| Realtime notification về UI | Integration Event → SignalR in Notifications module |
| Ghi audit log trong cùng transaction | Domain Event Handler |
