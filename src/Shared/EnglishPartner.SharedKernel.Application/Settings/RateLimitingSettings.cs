namespace EnglishPartner.SharedKernel.Settings;

public sealed class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";

    public RateLimitPolicy AIChat { get; set; } = new();
    public RateLimitPolicy AIScoring { get; set; } = new();
}

public sealed class RateLimitPolicy
{
    public int PermitLimit { get; set; } = 10;
    public int WindowSeconds { get; set; } = 60;
}
