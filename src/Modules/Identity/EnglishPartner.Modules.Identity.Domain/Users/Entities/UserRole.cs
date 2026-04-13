using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Identity.Domain.Users.Entities;

public sealed class UserRole : Entity
{
    private UserRole() { }

    public static UserRole Create(Guid userId, Guid roleId)
    {
        return new UserRole { Id = Guid.NewGuid(), UserId = userId, RoleId = roleId };
    }

    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
}
