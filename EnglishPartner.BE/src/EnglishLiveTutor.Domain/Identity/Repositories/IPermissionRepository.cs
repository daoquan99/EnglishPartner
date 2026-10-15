namespace EnglishLiveTutor.Domain.Identity.Repositories;

public interface IPermissionRepository
{
    Task<PermissionEntity?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<List<PermissionEntity>> ListAllAsync(CancellationToken ct = default);
    Task AddAsync(PermissionEntity permission, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
