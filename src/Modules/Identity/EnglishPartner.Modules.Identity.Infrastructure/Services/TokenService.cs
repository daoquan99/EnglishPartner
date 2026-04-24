using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EnglishPartner.Modules.Identity.Application.Abstractions;
using EnglishPartner.SharedKernel.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace EnglishPartner.Modules.Identity.Infrastructure.Services;

/// <summary>
/// JWT token generation service.
/// </summary>
internal sealed class TokenService(IOptions<JwtSettings> jwtSettings, TimeProvider timeProvider) : ITokenService
{
    public string GenerateAccessToken(Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissions, string sessionId)
    {
        var settings = jwtSettings.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("sid", sessionId),  // Redis session key — checked on every request by SessionValidationMiddleware
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(perm => new Claim("permission", perm)));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = timeProvider.GetUtcNow().UtcDateTime.AddMinutes(settings.AccessTokenExpirationMinutes),
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            SigningCredentials = credentials
        };

        var handler = new JsonWebTokenHandler();
        return handler.CreateToken(descriptor);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}
