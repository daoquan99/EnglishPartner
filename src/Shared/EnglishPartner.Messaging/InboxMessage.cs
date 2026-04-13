namespace EnglishPartner.Messaging;

/// <summary>
/// Inbox message used to ensure idempotency. 
/// Stored in DB to track successfully processed integration events.
/// </summary>
public sealed class InboxMessage
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public DateTime OccurredOn { get; set; }
    public DateTime? ProcessedOn { get; set; }
    public string? Error { get; set; }
}
