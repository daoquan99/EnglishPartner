namespace EnglishPartner.SharedKernel.Settings;

public sealed class CacheSettings
{
    public const string SectionName = "Cache";

    /// <summary>
    /// Global singleton instance mapped from appsettings.json
    /// </summary>
    public static CacheSettings Instance { get; private set; } = new();

    public static void Initialize(CacheSettings settings)
    {
        Instance = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// Redis key prefix for this application instance.
    /// </summary>
    public string InstanceName { get; set; } = "EnglishPartner:";
}
