namespace EnglishPartner.Messaging;

/// <summary>
/// Outbox message for reliable event delivery. Stored in DB and processed by background job.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public DateTime OccurredOn { get; set; }
    public DateTime? ProcessedOn { get; set; }
    public string? Error { get; set; }

    /// <summary>
    /// Number of processing attempts. Messages are retried until RetryCount reaches MaxRetries.
    /// </summary>
    public int RetryCount { get; set; }
}
