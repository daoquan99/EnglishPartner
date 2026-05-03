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

    public async Task<(List<string> RoleNames, List<string> Permissions)> GetRolesAndPermissionsAsync(
        IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default)
    {
        var ids = roleIds.ToList();

        // Query 1 of 1: role names (simple, fast — roles table is tiny)
        var roleNames = await context.Roles
            .AsNoTracking()
            .Where(r => ids.Contains(r.Id))
            .Select(r => r.Name)
            .ToListAsync(cancellationToken);

        // Query 2 of 1: permissions via JOIN (replaces GetPermissionCodesByRoleIdsAsync)
        var permissions = await context.RolePermissions
            .AsNoTracking()
            .Where(rp => ids.Contains(rp.RoleId))
            .Join(context.Permissions,
                  rp => rp.PermissionId,
                  p  => p.Id,
                  (rp, p) => p.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        return (roleNames, permissions);
    }

    public void Add(Role role) => context.Roles.Add(role);
    public void Update(Role role) => context.Roles.Update(role);
    public void Delete(Role role) => context.Roles.Remove(role);

    public async Task<List<Role>> GetAllWithPermissionsAsync(CancellationToken cancellationToken = default)
        => await context.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .ToListAsync(cancellationToken);
}

