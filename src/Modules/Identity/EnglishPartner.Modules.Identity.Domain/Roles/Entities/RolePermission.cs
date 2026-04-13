using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Identity.Domain.Roles.Entities;

public sealed class RolePermission : Entity
{
    private RolePermission() { }

    public static RolePermission Create(Guid roleId, Guid permissionId)
    {
        return new RolePermission { Id = Guid.NewGuid(), RoleId = roleId, PermissionId = permissionId };
    }

    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }
}
