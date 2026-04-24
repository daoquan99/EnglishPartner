using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Identity.Domain.Users.Entities;

public sealed class RefreshToken : Entity
{
    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, string token, DateTime expiresAt)
    {
        return new RefreshToken { Id = Guid.NewGuid(), UserId = userId, Token = token, ExpiresAt = expiresAt };
    }

    public Guid UserId { get; private set; }
    public string Token { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAt;
    public bool IsValid(DateTime utcNow) => !IsRevoked && !IsExpired(utcNow);

    public void Revoke() => IsRevoked = true;

    /// <summary>
    /// Rotates the token by replacing the current value with a new one.
    /// Keeps the same DB row — prevents unbounded growth from INSERT-on-refresh.
    /// After rotation, any request using the old token will fail IsValid() check.
    /// </summary>
    public void Rotate(string newToken, DateTime newExpiresAt)
    {
        Token = newToken;
        ExpiresAt = newExpiresAt;
        IsRevoked = false;
    }
}
