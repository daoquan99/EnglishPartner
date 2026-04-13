namespace EnglishPartner.SharedKernel.Settings;

public sealed class OutboxSettings
{
    public const string SectionName = "Outbox";

    /// <summary>
    /// How often (in seconds) the Quartz job polls pending outbox messages.
    /// Default: 10 seconds.
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// Maximum number of outbox messages to process per job execution.
    /// Default: 20.
    /// </summary>
    public int BatchSize { get; set; } = 20;
}
