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

    public async Task<User?> GetByExternalLoginAsync(string provider, string providerKey, CancellationToken cancellationToken = default)
        => await context.Users
            .Include(u => u.UserRoles)
            .Include(u => u.ExternalLogins)
            .FirstOrDefaultAsync(u => u.ExternalLogins.Any(el => el.LoginProvider == provider && el.ProviderKey == providerKey), cancellationToken);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await context.Users.AnyAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);

    public void Add(User user) => context.Users.Add(user);

    public void Update(User user) => context.Users.Update(user);
}
