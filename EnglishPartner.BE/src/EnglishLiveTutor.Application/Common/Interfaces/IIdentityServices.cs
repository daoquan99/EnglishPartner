using System.Security.Claims;
using EnglishLiveTutor.Domain.Identity;

namespace EnglishLiveTutor.Application.Common.Interfaces;

public interface IPasswordHasher
{
    string HashPassword(string rawPassword);
    bool VerifyPassword(string rawPassword, string passwordHash);
}

public sealed record GeneratedAccessToken(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc
);

public interface IJwtTokenService
{
    GeneratedAccessToken GenerateAccessToken(
        UserEntity user,
        IReadOnlyList<string> roleCodes,
        IReadOnlyList<string> permissionCodes);

    string GenerateRefreshTokenString();

    ClaimsPrincipal? ValidateAccessToken(string accessToken);
}
