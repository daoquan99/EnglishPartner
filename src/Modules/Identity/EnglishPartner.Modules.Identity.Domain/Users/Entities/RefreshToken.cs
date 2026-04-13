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
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsValid => !IsRevoked && !IsExpired;

    public void Revoke() => IsRevoked = true;
}
