namespace EnglishPartner.Modules.Identity.Application.Auth.Settings;

public sealed class SocialAuthSettings
{
    public const string SectionName = "SocialAuth";

    public GoogleAuthSettings Google { get; set; } = new();
    public MicrosoftAuthSettings Microsoft { get; set; } = new();
}

public sealed class GoogleAuthSettings
{
    public string ClientId { get; set; } = string.Empty;
}

public sealed class MicrosoftAuthSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string TenantId { get; set; } = "common";
}
