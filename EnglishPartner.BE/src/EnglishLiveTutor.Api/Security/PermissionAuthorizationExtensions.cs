using System.Security.Claims;

namespace EnglishLiveTutor.Api.Security;

public static class PermissionAuthorizationExtensions
{
    /// <summary>
    /// Enforces RBAC permission check on Backend Minimal API endpoints.
    /// Allows access if the authenticated user has the required permission claim or ADMIN role.
    /// </summary>
    public static RouteHandlerBuilder RequirePermission(
        this RouteHandlerBuilder builder,
        string requiredPermission)
    {
        return builder.AddEndpointFilter(async (invocationContext, next) =>
        {
            var httpContext = invocationContext.HttpContext;
            var user = httpContext.User;

            if (user?.Identity?.IsAuthenticated != true)
            {
                return Results.Json(
                    new { error = "Bạn cần đăng nhập để thực hiện thao tác này.", requiredPermission },
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            bool isAdmin = user.IsInRole("ADMIN") || user.IsInRole("Admin") || user.Claims.Any(c =>
                (c.Type == ClaimTypes.Role || c.Type == "role" || c.Type == "roles") &&
                string.Equals(c.Value, "ADMIN", StringComparison.OrdinalIgnoreCase));

            bool hasPerm = isAdmin || user.Claims.Any(c =>
                c.Type == "permission" &&
                string.Equals(c.Value, requiredPermission, StringComparison.OrdinalIgnoreCase));

            if (!hasPerm)
            {
                return Results.Json(
                    new { error = $"Bạn không có quyền '{requiredPermission}' để thực hiện thao tác này.", requiredPermission },
                    statusCode: StatusCodes.Status403Forbidden);
            }

            return await next(invocationContext);
        });
    }
}
