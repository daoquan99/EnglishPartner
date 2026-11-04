using System.Security.Claims;

namespace EnglishLiveTutor.Api.Security;

public static class ClaimsPrincipalPermissionExtensions
{
    public static bool HasPermission(this ClaimsPrincipal? user, string permission)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        // ADMIN bypass: kiểm tra claim role so sánh không phân biệt hoa thường
        bool isAdmin = user.Claims.Any(c =>
            (c.Type == ClaimTypes.Role || c.Type == "role" || c.Type == "roles") &&
            string.Equals(c.Value, "ADMIN", StringComparison.OrdinalIgnoreCase))
            || user.IsInRole("ADMIN")
            || user.IsInRole("Admin");

        if (isAdmin)
        {
            return true;
        }

        return user.Claims.Any(c =>
            (c.Type == "permission" || c.Type == "permissions") &&
            string.Equals(c.Value, permission, StringComparison.OrdinalIgnoreCase));
    }
}
