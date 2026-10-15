namespace EnglishLiveTutor.Domain.Identity.Permissions;

public interface IPermissionRepository
{
    Task<PermissionEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PermissionEntity?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<List<PermissionEntity>> GetByCodesAsync(IReadOnlyCollection<string> codes, CancellationToken ct = default);
    Task<List<PermissionEntity>> ListAllAsync(CancellationToken ct = default);
    Task AddAsync(PermissionEntity permission, CancellationToken ct = default);
}
