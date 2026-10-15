namespace EnglishLiveTutor.Domain.Identity.Users;

public interface IUserRepository
{
    Task<UserEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserEntity?> GetByIdWithRolesAndTokensAsync(Guid id, CancellationToken ct = default);
    Task<UserEntity?> GetByEmailOrUsernameWithDetailsAsync(string emailOrUsername, CancellationToken ct = default);
    Task<UserEntity?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<bool> ExistsByEmailOrUsernameAsync(string email, string username, CancellationToken ct = default);
    Task AddAsync(UserEntity user, CancellationToken ct = default);
}
