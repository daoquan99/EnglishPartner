namespace EnglishPartner.Modules.Identity.Domain.Roles.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<List<Role>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<List<string>> GetRoleNamesByIdsAsync(IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default);

    /// <summary>Returns permission codes for all given role IDs. Used to populate JWT permission claims.</summary>
    Task<List<string>> GetPermissionCodesByRoleIdsAsync(IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns role names AND permission codes in a single round-trip.
    /// Use on login/refresh to avoid 2 sequential DB queries on the hot auth path.
    /// </summary>
    Task<(List<string> RoleNames, List<string> Permissions)> GetRolesAndPermissionsAsync(
        IEnumerable<Guid> roleIds, CancellationToken cancellationToken = default);

    void Add(Role role);
    void Update(Role role);
    void Delete(Role role);

    /// <summary>Returns all roles with their permissions eagerly loaded.</summary>
    Task<List<Role>> GetAllWithPermissionsAsync(CancellationToken cancellationToken = default);
}
