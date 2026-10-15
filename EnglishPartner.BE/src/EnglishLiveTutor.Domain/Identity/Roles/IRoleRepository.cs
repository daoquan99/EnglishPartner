namespace EnglishLiveTutor.Domain.Identity.Roles;

public interface IRoleRepository
{
    Task<RoleEntity?> GetByIdWithPermissionsAsync(Guid id, CancellationToken ct = default);
    Task<RoleEntity?> GetByCodeWithPermissionsAsync(string code, CancellationToken ct = default);
    Task<List<RoleEntity>> GetActiveByCodesAsync(IReadOnlyCollection<string> codes, CancellationToken ct = default);
    Task<List<RoleEntity>> ListAllWithPermissionsAsync(CancellationToken ct = default);
    Task AddAsync(RoleEntity role, CancellationToken ct = default);
}
