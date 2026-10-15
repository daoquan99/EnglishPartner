using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Identity;
using Microsoft.Extensions.Configuration;

namespace EnglishLiveTutor.Infrastructure.Identity;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public string HashPassword(string rawPassword)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(rawPassword),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        return $"PBKDF2-SHA256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool VerifyPassword(string rawPassword, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(rawPassword) || string.IsNullOrWhiteSpace(passwordHash))
            return false;

        var parts = passwordHash.Split('$');
        if (parts.Length != 4 || parts[0] != "PBKDF2-SHA256")
            return false;

        if (!int.TryParse(parts[1], out int iterations))
            return false;

        byte[] salt;
        byte[] expectedHash;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expectedHash = Convert.FromBase64String(parts[3]);
        }
        catch
        {
            return false;
        }

        byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(rawPassword),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}

public sealed class HmacJwtTokenService : IJwtTokenService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenMinutes;
    private readonly TimeProvider _timeProvider;

    public HmacJwtTokenService(IConfiguration? configuration = null, TimeProvider? timeProvider = null)
    {
        _secretKey = configuration?["Jwt:SecretKey"]
            ?? "EnglishLiveTutor_Default_Super_Secret_Signing_Key_2026_AtLeast32Bytes!";
        _issuer = configuration?["Jwt:Issuer"] ?? "EnglishLiveTutor.Api";
        _audience = configuration?["Jwt:Audience"] ?? "EnglishLiveTutor.Client";
        _accessTokenMinutes = int.TryParse(configuration?["Jwt:AccessTokenMinutes"], out int mins) && mins > 0 ? mins : 60;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public GeneratedAccessToken GenerateAccessToken(
        UserEntity user,
        IReadOnlyList<string> roleCodes,
        IReadOnlyList<string> permissionCodes)
    {
        var now = _timeProvider.GetUtcNow();
        var expires = now.AddMinutes(_accessTokenMinutes);

        var header = new Dictionary<string, object>
        {
            ["alg"] = "HS256",
            ["typ"] = "JWT"
        };

        var payload = new Dictionary<string, object>
        {
            ["sub"] = user.Id.ToString(),
            ["unique_name"] = user.Username,
            ["email"] = user.Email,
            ["name"] = user.FullName,
            ["roles"] = roleCodes,
            ["permissions"] = permissionCodes,
            ["iss"] = _issuer,
            ["aud"] = _audience,
            ["iat"] = now.ToUnixTimeSeconds(),
            ["exp"] = expires.ToUnixTimeSeconds(),
            ["jti"] = Guid.NewGuid().ToString("N")
        };

        string encodedHeader = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(header)));
        string encodedPayload = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
        string unsignedToken = $"{encodedHeader}.{encodedPayload}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secretKey));
        byte[] sigBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(unsignedToken));
        string encodedSignature = Base64UrlEncode(sigBytes);

        return new GeneratedAccessToken($"{unsignedToken}.{encodedSignature}", expires);
    }

    public string GenerateRefreshTokenString()
    {
        return Base64UrlEncode(RandomNumberGenerator.GetBytes(48));
    }

    public ClaimsPrincipal? ValidateAccessToken(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            return null;

        string[] parts = accessToken.Trim().Split('.');
        if (parts.Length != 3)
            return null;

        string unsignedToken = $"{parts[0]}.{parts[1]}";
        byte[] keyBytes = Encoding.UTF8.GetBytes(_secretKey);
        byte[] expectedSig = HMACSHA256.HashData(keyBytes, Encoding.UTF8.GetBytes(unsignedToken));
        string expectedSigB64 = Base64UrlEncode(expectedSig);

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(parts[2]),
                Encoding.ASCII.GetBytes(expectedSigB64)))
        {
            return null;
        }

        try
        {
            byte[] payloadBytes = Base64UrlDecode(parts[1]);
            using var doc = JsonDocument.Parse(payloadBytes);
            var root = doc.RootElement;

            if (root.TryGetProperty("exp", out var expEl) && expEl.TryGetInt64(out long expUnix))
            {
                if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= expUnix)
                {
                    return null;
                }
            }

            var claims = new List<Claim>();
            if (root.TryGetProperty("sub", out var subEl) && subEl.GetString() is string sub)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, sub));
                claims.Add(new Claim("sub", sub));
            }
            if (root.TryGetProperty("unique_name", out var unEl) && unEl.GetString() is string un)
            {
                claims.Add(new Claim(ClaimTypes.Name, un));
                claims.Add(new Claim("unique_name", un));
            }
            if (root.TryGetProperty("email", out var emEl) && emEl.GetString() is string em)
            {
                claims.Add(new Claim(ClaimTypes.Email, em));
            }
            if (root.TryGetProperty("roles", out var rolesEl) && rolesEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in rolesEl.EnumerateArray())
                {
                    if (item.GetString() is string roleCode)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, roleCode));
                        claims.Add(new Claim("role", roleCode));
                    }
                }
            }
            if (root.TryGetProperty("permissions", out var permsEl) && permsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in permsEl.EnumerateArray())
                {
                    if (item.GetString() is string permCode)
                    {
                        claims.Add(new Claim("permission", permCode));
                    }
                }
            }

            var identity = new ClaimsIdentity(claims, "Bearer");
            return new ClaimsPrincipal(identity);
        }
        catch
        {
            return null;
        }
    }

    private static byte[] Base64UrlDecode(string input)
    {
        string padded = input.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Convert.FromBase64String(padded);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
