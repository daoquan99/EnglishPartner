using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Identity.Users.Entities;

public sealed class UserRoleEntity : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTimeOffset AssignedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    private UserRoleEntity()
    {
    }

    public UserRoleEntity(
        Guid userId,
        Guid roleId,
        DateTimeOffset? assignedAtUtc = null,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        UserId = userId;
        RoleId = roleId;
        AssignedAtUtc = assignedAtUtc ?? DateTimeOffset.UtcNow;
    }
}
