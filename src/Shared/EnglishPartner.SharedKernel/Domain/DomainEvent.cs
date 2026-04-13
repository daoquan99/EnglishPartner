namespace EnglishPartner.SharedKernel.Domain;

/// <summary>
/// Marker interface for domain events.
/// This is a pure domain abstraction — NOT coupled to MediatR.
/// MediatR integration (INotification) is handled in SharedKernel.Application via IDomainEventNotification.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}

/// <summary>
/// Base class for all domain events within a bounded context.
/// Pure — no framework dependencies.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
