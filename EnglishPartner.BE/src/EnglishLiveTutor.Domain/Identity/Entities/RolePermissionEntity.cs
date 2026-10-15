using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Identity.Entities;

public sealed class RolePermissionEntity : Entity<Guid>
{
    public Guid RoleId { get; private set; }
    public Guid PermissionId { get; private set; }
    public DateTimeOffset GrantedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    private RolePermissionEntity()
    {
    }

    public RolePermissionEntity(
        Guid roleId,
        Guid permissionId,
        DateTimeOffset? grantedAtUtc = null,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        RoleId = roleId;
        PermissionId = permissionId;
        GrantedAtUtc = grantedAtUtc ?? DateTimeOffset.UtcNow;
    }
}
