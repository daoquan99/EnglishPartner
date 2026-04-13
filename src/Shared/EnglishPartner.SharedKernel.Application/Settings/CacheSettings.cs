namespace EnglishPartner.SharedKernel.Settings;

public sealed class CacheSettings
{
    public const string SectionName = "Cache";

    /// <summary>
    /// Redis key prefix for this application instance.
    /// </summary>
    public string InstanceName { get; set; } = "EnglishPartner:";
}
