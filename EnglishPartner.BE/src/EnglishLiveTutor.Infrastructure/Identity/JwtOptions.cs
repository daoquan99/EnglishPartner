namespace EnglishLiveTutor.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string? SecretKey { get; set; }
    public string Issuer { get; set; } = "EnglishLiveTutor.Api";
    public string Audience { get; set; } = "EnglishLiveTutor.Client";
    public int AccessTokenMinutes { get; set; } = 60;
}
