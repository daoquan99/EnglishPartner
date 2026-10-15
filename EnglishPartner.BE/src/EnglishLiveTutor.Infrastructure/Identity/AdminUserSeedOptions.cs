namespace EnglishLiveTutor.Infrastructure.Identity;

public sealed class AdminUserSeedOptions
{
    public const string SectionName = "AdminUser";

    public string Username { get; set; } = "admin";
    public string Email { get; set; } = "admin@lingomint.local";
    public string Password { get; set; } = "Admin@123";
    public string FullName { get; set; } = "LingoMint Administrator";
    public string PreferredCefrLevel { get; set; } = "C1";
    public string PreferredVoiceName { get; set; } = "Aoede";
}
