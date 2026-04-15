using EnglishPartner.Modules.Identity.Domain.Users;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Identity.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(IdentityDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Users
            .Include(u => u.UserRoles)
            .Include(u => u.ExternalLogins)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await context.Users
            .Include(u => u.UserRoles)
            .Include(u => u.ExternalLogins)
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);

    /// <summary>Lean auth variant — only loads UserRoles, no ExternalLogins join.</summary>
    public async Task<User?> GetByEmailForAuthAsync(string email, CancellationToken cancellationToken = default)
        => await context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);

    public async Task<User?> GetByExternalLoginAsync(string provider, string providerKey, CancellationToken cancellationToken = default)
        => await context.Users
            .Include(u => u.UserRoles)
            .Include(u => u.ExternalLogins)
            .FirstOrDefaultAsync(u => u.ExternalLogins.Any(el => el.LoginProvider == provider && el.ProviderKey == providerKey), cancellationToken);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await context.Users.AnyAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);

    public void Add(User user)    => context.Users.Add(user);
    public void Update(User user) => context.Users.Update(user);
    public void Delete(User user) => context.Users.Remove(user);

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        string? search,
        string? role,
        bool?   isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Users
            .AsNoTracking()                  // read-only admin list — no tracking needed
            .Include(u => u.UserRoles)
            .Include(u => u.ExternalLogins)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            // ILike = case-insensitive LIKE in PostgreSQL (sargable with pg_trgm index)
            query = query.Where(u =>
                EF.Functions.ILike(u.Email, term) ||
                EF.Functions.ILike(u.DisplayName, term));
        }

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleId = await context.Roles
                .Where(r => r.Name == role)
                .Select(r => r.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (roleId == Guid.Empty)
                return ([], 0); // role doesn't exist → no users match

            query = query.Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

}
