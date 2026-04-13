namespace EnglishPartner.Modules.Identity.Domain.Users.Entities;

public sealed class UserExternalLogin
{
    private UserExternalLogin() { }

    internal UserExternalLogin(string loginProvider, string providerKey, string? providerDisplayName)
    {
        LoginProvider = loginProvider;
        ProviderKey = providerKey;
        ProviderDisplayName = providerDisplayName;
    }

    public Guid UserId { get; private set; }
    
    // e.g. "Google", "Microsoft"
    public string LoginProvider { get; private set; } = string.Empty;
    
    // e.g. The 'sub' claim from Google, 'oid' from Microsoft
    public string ProviderKey { get; private set; } = string.Empty;

    public string? ProviderDisplayName { get; private set; }
}
