using EnglishPartner.SharedKernel.Domain;
using EnglishPartner.Modules.Identity.Domain.Roles.Entities;

namespace EnglishPartner.Modules.Identity.Domain.Roles;

public sealed class Role : AggregateRoot
{
    private readonly List<RolePermission> _rolePermissions = [];

    private Role() { }

    public static Role Create(string name, string? description = null)
    {
        return new Role { Id = Guid.NewGuid(), Name = name, Description = description };
    }

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    public IReadOnlyList<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public void AssignPermission(Guid permissionId)
    {
        if (_rolePermissions.Any(rp => rp.PermissionId == permissionId)) return;
        _rolePermissions.Add(RolePermission.Create(Id, permissionId));
    }

    public void RemovePermission(Guid permissionId)
    {
        var rp = _rolePermissions.FirstOrDefault(x => x.PermissionId == permissionId);
        if (rp is not null) _rolePermissions.Remove(rp);
    }
}
