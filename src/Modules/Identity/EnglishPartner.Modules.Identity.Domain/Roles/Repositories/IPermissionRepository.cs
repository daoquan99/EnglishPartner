using EnglishPartner.Modules.Identity.Domain.Roles.Entities;

namespace EnglishPartner.Modules.Identity.Domain.Roles.Repositories;

public interface IPermissionRepository
{
    Task<Permission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Permission>> GetAllAsync(CancellationToken cancellationToken = default);
}
