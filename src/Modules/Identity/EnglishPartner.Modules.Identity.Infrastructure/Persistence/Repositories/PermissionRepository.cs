using EnglishPartner.Modules.Identity.Domain.Roles.Entities;
using EnglishPartner.Modules.Identity.Domain.Roles.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Identity.Infrastructure.Persistence.Repositories;

internal sealed class PermissionRepository(IdentityDbContext context) : IPermissionRepository
{
    public async Task<Permission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Permissions.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<List<Permission>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.Permissions.AsNoTracking().OrderBy(p => p.Module).ThenBy(p => p.Code).ToListAsync(cancellationToken);
}
