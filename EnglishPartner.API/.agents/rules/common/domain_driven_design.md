---
trigger: manual
---

# Common: Domain Driven Design (DDD)

> **Scope:** AggregateRoot, Entity, ValueObject, Domain Events
> @mention this file when working on: domain models, entities, relationships

---

## 1. AggregateRoot vs Entity

### `Entity`
- Là class cơ sở cho mọi thực thể có định danh (`Id`).
- So sánh bằng nhau (Equality) dựa trên `Id`.
- **Tích hợp sẵn Audit:** Có chứa `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`. Các trường này sẽ được `AuditableEntityInterceptor` tự động điền khi lưu vào DB.
- **Entity con:** Các Entity không phải AggregateRoot chỉ sống bên trong một AggregateRoot. Không được phép truy vấn hay lưu trữ nó độc lập (không có Repository riêng).

### `AggregateRoot`
- Kế thừa từ `Entity`. Là gốc của một cụm dữ liệu (Transaction Boundary).
- Repositories (`IRepository<T>`) **CHỈ** được tạo cho AggregateRoots.
- **Tích hợp Soft-Delete:** Có sẵn `IsDeleted`, `DeletedAt`, `DeletedBy`, `IsActive`. Cung cấp hàm `SoftDelete()` và `Restore()`.
- **Domain Events:** Cung cấp hàm `RaiseDomainEvent(domainEvent)` để phát hành event khi có state thay đổi.

```csharp
public sealed class Vocabulary : AggregateRoot
{
    public string Word { get; private set; }
    
    private Vocabulary() { } // EF Core
    
    public static Vocabulary Create(string word)
    {
        var entity = new Vocabulary { Id = Guid.NewGuid(), Word = word };
        entity.RaiseDomainEvent(new VocabularyCreatedDomainEvent(entity.Id));
        return entity;
    }
}
```

---

## 2. Relationships (Foreign Keys vs Navigation Properties)

**Quy tắc vàng (CRITICAL RULE):** KHÔNG dùng Object Navigation Properties để tham chiếu đến một `AggregateRoot` khác. Chỉ tham chiếu bằng `Guid Id`.

**SAI (Vi phạm ranh giới DDD):**
```csharp
public class StudySession : AggregateRoot
{
    public User User { get; private set; } // BAD: Navigation property sang AggregateRoot khác
}
```

**ĐÚNG (Decoupled):**
```csharp
public class StudySession : AggregateRoot
{
    public Guid UserId { get; private set; } // GOOD: Reference by Id only
}
```
*Tại sao?* Tránh việc EF Core tự động eager-load toàn bộ database, giữ cho các Aggregate độc lập hoàn toàn, dễ dàng bóc tách thành Microservices sau này. Navigation properties CHỈ được dùng cho Entity con hoặc ValueObject bên trong cùng một AggregateRoot.

---

## 3. Domain Events (`DomainEvent`)

- Khác với Integration Event, Domain Event chỉ xảy ra bên trong 1 module (In-process).
- Phải kế thừa record `DomainEvent`. File base này là **Pure Domain** (hoàn toàn không dính dáng đến framework hay MediatR).
- Nó có sẵn `EventId` và `OccurredAt`.

```csharp
// ĐỊNH NGHĨA EVENT
public sealed record VocabularyCreatedDomainEvent(Guid VocabularyId) : DomainEvent;

// PHÁT HÀNH EVENT (bên trong AggregateRoot)
RaiseDomainEvent(new VocabularyCreatedDomainEvent(Id));
```

**Cách xử lý (MediatR Integration):**
Tầng `Application` có class `DomainEventNotification<T>`. Interceptor sẽ tự động bọc Domain Event lại và quăng vào MediatR. Bạn xử lý nó bằng `INotificationHandler`:

```csharp
internal sealed class VocabularyCreatedHandler : INotificationHandler<DomainEventNotification<VocabularyCreatedDomainEvent>>
{
    public Task Handle(DomainEventNotification<VocabularyCreatedDomainEvent> notification, CancellationToken ct)
    {
        var domainEvent = notification.DomainEvent;
        // logic...
    }
}
```

---

## 4. Value Objects (`ValueObject`)

Dùng cho những khái niệm không có định danh (`Id`), chỉ quan tâm đến giá trị.
- Bắt buộc phải override `GetEqualityComponents()` để so sánh từng thuộc tính.

```csharp
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```
