namespace EnglishPartner.Messaging;

/// <summary>
/// Base class for integration events that cross module boundaries.
/// </summary>
public abstract record IntegrationEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
