using EnglishLiveTutor.Api.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

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
                return ApiProblem.Unauthorized(
                    "Bạn cần đăng nhập để thực hiện thao tác này.",
                    httpContext,
                    new Dictionary<string, object?> { ["requiredPermission"] = requiredPermission });
            }

            if (!user.HasPermission(requiredPermission))
            {
                return ApiProblem.Forbidden(
                    $"Bạn không có quyền '{requiredPermission}' để thực hiện thao tác này.",
                    httpContext,
                    new Dictionary<string, object?> { ["requiredPermission"] = requiredPermission });
            }

            return await next(invocationContext);
        });
    }
}
