namespace EnglishPartner.Modules.Identity.Application.Abstractions;

/// <summary>
/// Abstraction for JWT token generation — implemented in Infrastructure.
/// </summary>
public interface ITokenService
{
    string GenerateAccessToken(Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissions);
    string GenerateRefreshToken();
}
