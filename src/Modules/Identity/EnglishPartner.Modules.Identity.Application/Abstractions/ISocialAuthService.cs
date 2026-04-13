namespace EnglishPartner.Modules.Identity.Application.Abstractions;

public interface ISocialAuthService
{
    /// <summary>
    /// Verifies the provider's token and returns the extracted user information.
    /// Throws or returns failure if token is invalid or expired.
    /// </summary>
    Task<SocialUserInfo?> VerifyTokenAsync(string provider, string token, CancellationToken cancellationToken = default);
}

public sealed record SocialUserInfo(
    string Provider,
    string ProviderUserId,
    string Email,
    string? FirstName,
    string? LastName,
    string? AvatarUrl);
