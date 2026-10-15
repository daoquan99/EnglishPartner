using System.Security.Claims;
using EnglishLiveTutor.Application.Common.Interfaces;

namespace EnglishLiveTutor.Api.Services;

public sealed class ClaimsPermissionChecker(IHttpContextAccessor httpContextAccessor) : IPermissionChecker
{
    public Task<bool> HasPermissionAsync(string permission, CancellationToken ct = default)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return Task.FromResult(false);
        }

        bool isAdmin = user.Claims.Any(c =>
            (c.Type == ClaimTypes.Role || c.Type == "role") &&
            string.Equals(c.Value, "ADMIN", StringComparison.OrdinalIgnoreCase));

        bool hasPerm = isAdmin || user.Claims.Any(c =>
            c.Type == "permission" &&
            string.Equals(c.Value, permission, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(hasPerm);
    }
}
