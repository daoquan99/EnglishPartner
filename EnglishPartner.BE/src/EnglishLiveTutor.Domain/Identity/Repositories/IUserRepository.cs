using EnglishLiveTutor.Domain.Identity.Entities;

namespace EnglishLiveTutor.Domain.Identity.Repositories;

public interface IUserRepository
{
    Task<UserEntity?> GetByIdWithRolesAndTokensAsync(Guid id, CancellationToken ct = default);
    Task<UserEntity?> GetByEmailOrUsernameWithDetailsAsync(string emailOrUsername, CancellationToken ct = default);
    Task<UserEntity?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<bool> ExistsByEmailOrUsernameAsync(string email, string username, CancellationToken ct = default);
    Task AddAsync(UserEntity user, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task<int> RevokeAllActiveRefreshTokensAsync(Guid userId, DateTimeOffset revokedAtUtc, string reason = "Reuse detected", CancellationToken ct = default);
    Task PurgeExpiredOrRevokedRefreshTokensAsync(Guid userId, DateTimeOffset nowUtc, CancellationToken ct = default);
    Task<bool> RotateRefreshTokenConditionallyAsync(Guid oldTokenId, RefreshTokenEntity newToken, DateTimeOffset nowUtc, CancellationToken ct = default);
    Task<RefreshTokenEntity?> GetRefreshTokenByIdAsync(Guid id, CancellationToken ct = default);
}
