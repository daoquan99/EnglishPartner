using EnglishPartner.Modules.Identity.Domain.Roles;
using EnglishPartner.Modules.Identity.Domain.Roles.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Identity.Infrastructure.Persistence.Repositories;

internal sealed class RoleRepository(IdentityDbContext context) : IRoleRepository
{
    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        => await context.Roles.FirstOrDefaultAsync(r => r.Name == name, cancellationToken);

    public async Task<List<Role>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.Roles.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
        => await context.Roles.AnyAsync(r => r.Name == name, cancellationToken);

    public async Task<List<string>> GetRoleNamesByIdsAsync(IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default)
        => await context.Roles
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Name)
            .ToListAsync(cancellationToken);

    public async Task<List<string>> GetPermissionCodesByRoleIdsAsync(
        IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        var ids = roleIds.ToList();
        return await context.RolePermissions
            .Where(rp => ids.Contains(rp.RoleId))
            .Join(context.Permissions,
                  rp => rp.PermissionId,
                  p  => p.Id,
                  (rp, p) => p.Code)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public void Add(Role role) => context.Roles.Add(role);

    public void Update(Role role) => context.Roles.Update(role);
}


