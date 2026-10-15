using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Identity.Users.Entities;

public sealed class RefreshTokenEntity : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? RevokedReason { get; private set; }
    public string? ReplacedByToken { get; private set; }
    public string? CreatedByIp { get; private set; }
    public string? DeviceInfo { get; private set; }

    public bool IsRevoked => RevokedAtUtc.HasValue;

    private RefreshTokenEntity()
    {
    }

    public RefreshTokenEntity(
        Guid userId,
        string token,
        DateTimeOffset expiresAtUtc,
        string? createdByIp = null,
        string? deviceInfo = null,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("RefreshToken không được để trống.");

        UserId = userId;
        Token = token.Trim();
        ExpiresAtUtc = expiresAtUtc;
        CreatedByIp = string.IsNullOrWhiteSpace(createdByIp) ? null : createdByIp.Trim();
        DeviceInfo = string.IsNullOrWhiteSpace(deviceInfo) ? null : deviceInfo.Trim();
    }

    public bool IsExpired(DateTimeOffset nowUtc) => nowUtc >= ExpiresAtUtc;

    public bool IsActive(DateTimeOffset nowUtc) => !IsRevoked && !IsExpired(nowUtc);

    public void Revoke(DateTimeOffset revokedAtUtc, string? reason = null, string? replacedByToken = null)
    {
        if (RevokedAtUtc.HasValue)
        {
            return;
        }

        RevokedAtUtc = revokedAtUtc;
        RevokedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        ReplacedByToken = string.IsNullOrWhiteSpace(replacedByToken) ? null : replacedByToken.Trim();
    }
}
