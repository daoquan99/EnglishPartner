using EnglishPartner.Modules.Identity.Domain.Users.Entities;

namespace EnglishPartner.Modules.Identity.Domain.Users.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    void Add(RefreshToken refreshToken);
    void Update(RefreshToken refreshToken);
    Task RevokeAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
