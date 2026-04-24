namespace EnglishPartner.Modules.Identity.Domain.Users.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lean variant for the password login path — loads only UserRoles, skips ExternalLogins.
    /// Do NOT use for social login flows that need ExternalLogins.
    /// </summary>
    Task<User?> GetByEmailForAuthAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetByExternalLoginAsync(string provider, string providerKey, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    void Add(User user);
    void Update(User user);
    void Delete(User user);

    // Admin queries
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        string? search,
        string? role,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Returns true if any user has the given roleId — used to guard role deletion.</summary>
    Task<bool> AnyWithRoleAsync(Guid roleId, CancellationToken cancellationToken = default);
}
