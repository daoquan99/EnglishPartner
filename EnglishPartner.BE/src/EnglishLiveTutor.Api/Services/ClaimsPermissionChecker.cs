using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Application.Common.Interfaces;

namespace EnglishLiveTutor.Api.Services;

public sealed class ClaimsPermissionChecker(IHttpContextAccessor httpContextAccessor) : IPermissionChecker
{
    public Task<bool> HasPermissionAsync(string permission, CancellationToken ct = default)
    {
        var user = httpContextAccessor.HttpContext?.User;
        return Task.FromResult(user.HasPermission(permission));
    }
}
