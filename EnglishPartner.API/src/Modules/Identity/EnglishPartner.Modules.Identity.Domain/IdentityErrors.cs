using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Identity.Domain;

/// <summary>
/// Domain errors for the Identity module.
/// Error codes use "Identity.X.Y" namespace format (e.g. "Identity.User.NotFound"),
/// intentionally different from <see cref="CommonErrors"/> (e.g. "User.NotFound").
/// Do NOT migrate to CommonErrors — changing codes would be a breaking change for clients.
/// </summary>
public static class IdentityErrors
{
    public static class User
    {
        public static readonly Error EmailAlreadyExists =
            new("Identity.User.EmailAlreadyExists", "A user with this email already exists.");

        public static readonly Error NotFound =
            new("Identity.User.NotFound", "User was not found.");

        public static Error NotFoundById(Guid id) =>
            new("Identity.User.NotFound", $"User with Id '{id}' was not found.");

        public static readonly Error InvalidCredentials =
            new("Identity.User.InvalidCredentials", "Invalid email or password.");

        public static readonly Error Deactivated =
            new("Identity.User.Deactivated", "This account has been deactivated.");

        public static readonly Error OnboardingAlreadyCompleted =
            new("Identity.User.OnboardingAlreadyCompleted", "Onboarding has already been completed.");
    }

    public static class Token
    {
        public static readonly Error InvalidRefreshToken =
            new("Identity.Token.InvalidRefreshToken", "The refresh token is invalid or expired.");

        public static readonly Error Expired =
            new("Identity.Token.Expired", "The token has expired.");
    }

    public static class Role
    {
        public static readonly Error NotFound =
            new("Identity.Role.NotFound", "Role was not found.");

        public static readonly Error NameAlreadyExists =
            new("Identity.Role.NameAlreadyExists", "A role with this name already exists.");

        public static readonly Error HasUsers =
            new("Identity.Role.HasUsers", "Cannot delete a role that is still assigned to users.");

        public static readonly Error PermissionAlreadyAssigned =
            new("Identity.Role.PermissionAlreadyAssigned", "This permission is already assigned to the role.");

        public static readonly Error PermissionNotAssigned =
            new("Identity.Role.PermissionNotAssigned", "This permission is not assigned to the role.");
    }

    public static class Permission
    {
        public static Error NotFoundById(Guid id) =>
            new("Identity.Permission.NotFound", $"Permission with Id '{id}' was not found.");
    }

    public static class Streak
    {
        public static readonly Error AlreadyCheckedIn =
            new("Identity.Streak.AlreadyCheckedIn", "You have already checked in today.");

        public static readonly Error NotFound =
            new("Identity.Streak.NotFound", "Streak record was not found.");
    }

    public static class Auth
    {
        public static readonly Error InvalidSocialToken =
            new("Auth.SocialLogin.InvalidToken", "Invalid or expired social token.");
    }

    public static class PasswordReset
    {
        public static readonly Error InvalidToken =
            new("Identity.PasswordReset.InvalidToken", "The password reset token is invalid or expired.");
    }
}
