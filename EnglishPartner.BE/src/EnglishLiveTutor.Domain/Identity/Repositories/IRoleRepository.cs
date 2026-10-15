namespace EnglishLiveTutor.Domain.Identity.Repositories;

public interface IRoleRepository
{
    Task<RoleEntity?> GetByIdWithPermissionsAsync(Guid id, CancellationToken ct = default);
    Task<RoleEntity?> GetByCodeWithPermissionsAsync(string code, CancellationToken ct = default);
    Task<List<RoleEntity>> ListAllWithPermissionsAsync(CancellationToken ct = default);
    Task AddAsync(RoleEntity role, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
