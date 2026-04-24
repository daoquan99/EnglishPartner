using EnglishPartner.Modules.Identity.Domain.Users;

namespace EnglishPartner.Domain.Tests.Identity;

public sealed class UserTests
{
    private static User CreateUser(string email = "test@example.com",
                                   string passwordHash = "hashed_password",
                                   string displayName = "Test User") =>
        User.Create(email, passwordHash, displayName);

    // ── Create ────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldInitialise_WithCorrectProperties()
    {
        var user = CreateUser();

        user.Email.Should().Be("test@example.com");
        user.DisplayName.Should().Be("Test User");
        user.PasswordHash.Should().Be("hashed_password");
        user.IsOnboardingCompleted.Should().BeFalse();
        user.IsActive.Should().BeTrue();
        user.IsDeleted.Should().BeFalse();
        user.UserRoles.Should().BeEmpty();
        user.ExternalLogins.Should().BeEmpty();
    }

    // ── CompleteOnboarding ────────────────────────────────────────────────

    [Fact]
    public void CompleteOnboarding_ShouldSetFlagToTrue()
    {
        var user = CreateUser();

        user.CompleteOnboarding("beginner", "fluency", "en", 15, null, null, 0);

        user.IsOnboardingCompleted.Should().BeTrue();
    }

    // ── UpdateProfile ─────────────────────────────────────────────────────

    [Fact]
    public void UpdateProfile_ShouldChangeDisplayName()
    {
        var user = CreateUser();

        user.UpdateProfile("New Name");

        user.DisplayName.Should().Be("New Name");
    }

    // ── ChangePassword ────────────────────────────────────────────────────

    [Fact]
    public void ChangePassword_ShouldUpdatePasswordHash()
    {
        var user = CreateUser();

        user.ChangePassword("new_hashed_password");

        user.PasswordHash.Should().Be("new_hashed_password");
    }

    // ── AssignRole / RemoveRole ───────────────────────────────────────────

    [Fact]
    public void AssignRole_ShouldAddRoleToUser()
    {
        var user   = CreateUser();
        var roleId = Guid.NewGuid();

        user.AssignRole(roleId);

        user.UserRoles.Should().ContainSingle(r => r.RoleId == roleId);
    }

    [Fact]
    public void AssignRole_Duplicate_ShouldNotAddTwice()
    {
        var user   = CreateUser();
        var roleId = Guid.NewGuid();

        user.AssignRole(roleId);
        user.AssignRole(roleId); // second call — duplicate

        user.UserRoles.Should().ContainSingle(r => r.RoleId == roleId,
            because: "duplicate role assignments should be idempotent");
    }

    [Fact]
    public void RemoveRole_ShouldRemoveRoleFromUser()
    {
        var user   = CreateUser();
        var roleId = Guid.NewGuid();
        user.AssignRole(roleId);

        user.RemoveRole(roleId);

        user.UserRoles.Should().NotContain(r => r.RoleId == roleId);
    }

    // ── SoftDelete ────────────────────────────────────────────────────────

    [Fact]
    public void SoftDelete_ShouldMarkUserAsDeleted_AndInactive()
    {
        var user    = CreateUser();
        var adminId = Guid.NewGuid();

        user.SoftDelete(adminId);

        user.IsDeleted.Should().BeTrue();
        user.IsActive.Should().BeFalse();
        user.DeletedBy.Should().Be(adminId);
        user.DeletedAt.Should().NotBeNull();
    }

    // ── AddExternalLogin ──────────────────────────────────────────────────

    [Fact]
    public void AddExternalLogin_ShouldAddProvider()
    {
        var user = CreateUser();

        user.AddExternalLogin("Google", "google-key-123", "Google Name");

        user.ExternalLogins.Should().ContainSingle(e =>
            e.LoginProvider == "Google" && e.ProviderKey == "google-key-123");
    }
}
