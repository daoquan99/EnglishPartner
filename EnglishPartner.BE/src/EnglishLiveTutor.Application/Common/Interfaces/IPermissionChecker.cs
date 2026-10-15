namespace EnglishLiveTutor.Application.Common.Interfaces;

/// <summary>
/// Abstraction for checking user permissions.
/// </summary>
public interface IPermissionChecker
{
    Task<bool> HasPermissionAsync(string permission, CancellationToken ct = default);
}
