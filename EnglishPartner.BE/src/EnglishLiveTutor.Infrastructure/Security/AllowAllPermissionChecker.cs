using EnglishLiveTutor.Application.Common.Interfaces;

namespace EnglishLiveTutor.Infrastructure.Security;

public sealed class AllowAllPermissionChecker : IPermissionChecker
{
    public static readonly AllowAllPermissionChecker Instance = new();

    public Task<bool> HasPermissionAsync(string permission, CancellationToken ct = default)
        => Task.FromResult(true);
}
