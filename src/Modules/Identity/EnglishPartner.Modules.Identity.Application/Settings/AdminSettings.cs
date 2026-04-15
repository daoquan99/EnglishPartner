namespace EnglishPartner.Modules.Identity.Application.Settings;

/// <summary>
/// Configuration for the default admin account seeded on first run.
/// Bound from appsettings.Development.json section "Admin".
/// </summary>
public sealed class AdminSettings
{
    public const string SectionName = "Admin";

    public string Email       { get; set; } = "admin@englishpartner.com";
    public string Password    { get; set; } = string.Empty;
    public string DisplayName { get; set; } = "Super Admin";
}
