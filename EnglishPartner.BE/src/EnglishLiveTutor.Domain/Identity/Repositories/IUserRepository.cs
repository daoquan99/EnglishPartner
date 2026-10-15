namespace EnglishLiveTutor.Domain.Identity.Repositories;

public interface IUserRepository
{
    Task<UserEntity?> GetByIdWithRolesAndTokensAsync(Guid id, CancellationToken ct = default);
    Task<UserEntity?> GetByEmailOrUsernameWithDetailsAsync(string emailOrUsername, CancellationToken ct = default);
    Task<UserEntity?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<bool> ExistsByEmailOrUsernameAsync(string email, string username, CancellationToken ct = default);
    Task AddAsync(UserEntity user, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
