namespace EnglishPartner.Modules.Identity.Application.Abstractions;

/// <summary>
/// Abstraction for JWT token generation — implemented in Infrastructure.
/// </summary>
public interface ITokenService
{
    /// <param name="sessionId">
    /// Redis session ID embedded as 'sid' claim.
    /// SessionValidationMiddleware checks this on every authenticated request
    /// to immediately invalidate tokens when the session is deleted (logout).
    /// </param>
    string GenerateAccessToken(Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissions, string sessionId);

    string GenerateRefreshToken();
}
