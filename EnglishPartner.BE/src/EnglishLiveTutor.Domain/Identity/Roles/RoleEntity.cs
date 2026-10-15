using EnglishLiveTutor.Domain.Common.Primitives;
using EnglishLiveTutor.Domain.Identity.Roles.Entities;

namespace EnglishLiveTutor.Domain.Identity.Roles;

public sealed class RoleEntity : AggregateRoot<Guid>
{
    private List<RolePermissionEntity> _rolePermissions = new();

    public string Code { get; private set; } = string.Empty; // ADMIN | TEACHER | LEARNER
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsSystemRole { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<RolePermissionEntity> RolePermissions => _rolePermissions.AsReadOnly();

    private RoleEntity()
    {
    }

    public RoleEntity(
        string code,
        string name,
        string description = "",
        bool isSystemRole = false,
        bool isActive = true,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("Mã vai trò (Role Code) không được để trống.");
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Tên vai trò không được để trống.");

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        IsSystemRole = isSystemRole;
        IsActive = isActive;
    }

    public void UpdateDetails(string name, string description)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
    }

    public RolePermissionEntity GrantPermission(Guid permissionId, DateTimeOffset? grantedAtUtc = null)
    {
        var existing = _rolePermissions.FirstOrDefault(rp => rp.PermissionId == permissionId);
        if (existing is not null)
        {
            return existing;
        }

        var rp = new RolePermissionEntity(Id, permissionId, grantedAtUtc ?? DateTimeOffset.UtcNow);
        _rolePermissions.Add(rp);
        return rp;
    }

    public void SetPermissions(IEnumerable<Guid> permissionIds, DateTimeOffset? grantedAtUtc = null)
    {
        _rolePermissions.Clear();
        foreach (var permissionId in permissionIds.Distinct())
        {
            GrantPermission(permissionId, grantedAtUtc);
        }
    }

    public override void SoftDelete(DateTimeOffset deletedAtUtc, string? deletedBy = null)
    {
        if (IsSystemRole)
        {
            throw new InvalidOperationException($"Không được phép xóa vai trò hệ thống '{Code}'.");
        }

        base.SoftDelete(deletedAtUtc, deletedBy);
    }
}
