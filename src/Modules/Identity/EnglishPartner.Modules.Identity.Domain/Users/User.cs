using EnglishPartner.SharedKernel.Domain;
using EnglishPartner.Modules.Identity.Domain.Users.Entities;
using EnglishPartner.Modules.Identity.Domain.Users.Events;

namespace EnglishPartner.Modules.Identity.Domain.Users;

/// <summary>
/// User aggregate root — central entity for authentication and identity.
/// </summary>
public sealed class User : AggregateRoot
{
    private readonly List<UserRole> _userRoles = [];

    private User() { }

    public static User Create(string email, string passwordHash, string displayName)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            PasswordHash = passwordHash,
            DisplayName = displayName
        };

        user.RaiseDomainEvent(new UserRegisteredDomainEvent(user.Id, user.Email, user.DisplayName));
        return user;
    }

    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public bool IsOnboardingCompleted { get; private set; }

    public IReadOnlyList<UserRole> UserRoles => _userRoles.AsReadOnly();

    public void CompleteOnboarding()
    {
        IsOnboardingCompleted = true;
        RaiseDomainEvent(new OnboardingCompletedDomainEvent(Id));
    }

    public void UpdateProfile(string displayName) => DisplayName = displayName;

    public void ChangePassword(string newPasswordHash) => PasswordHash = newPasswordHash;

    public void AssignRole(Guid roleId)
    {
        if (_userRoles.Any(ur => ur.RoleId == roleId)) return;
        _userRoles.Add(UserRole.Create(Id, roleId));
    }

    public void RemoveRole(Guid roleId)
    {
        var userRole = _userRoles.FirstOrDefault(ur => ur.RoleId == roleId);
        if (userRole is not null) _userRoles.Remove(userRole);
    }
}
