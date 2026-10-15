using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity;

public static class IdentityProjectionHelper
{
    public static async Task<(List<string> RoleCodes, List<string> PermissionCodes)> ResolveUserRolesAndPermissionsAsync(
        IAppDbContext db,
        Guid userId,
        CancellationToken ct = default)
    {
        var roleIds = await db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(ct);

        if (roleIds.Count == 0)
        {
            return (new List<string>(), new List<string>());
        }

        var roles = await db.Roles
            .AsNoTracking()
            .Where(r => roleIds.Contains(r.Id) && r.IsActive)
            .ToListAsync(ct);

        var activeRoleIds = roles.Select(r => r.Id).ToList();
        var roleCodes = roles.Select(r => r.Code).OrderBy(c => c).ToList();

        var permissionIds = await db.RolePermissions
            .AsNoTracking()
            .Where(rp => activeRoleIds.Contains(rp.RoleId))
            .Select(rp => rp.PermissionId)
            .Distinct()
            .ToListAsync(ct);

        var permissionCodes = await db.Permissions
            .AsNoTracking()
            .Where(p => permissionIds.Contains(p.Id))
            .Select(p => p.Code)
            .OrderBy(c => c)
            .ToListAsync(ct);

        return (roleCodes, permissionCodes);
    }

    public static UserProfileDto MapToProfileDto(
        UserEntity user,
        List<string> roleCodes,
        List<string> permissionCodes) =>
        new(
            Id: user.Id,
            Username: user.Username,
            Email: user.Email,
            FullName: user.FullName,
            AvatarUrl: user.AvatarUrl,
            PreferredCefrLevel: user.PreferredCefrLevel,
            PreferredVoiceName: user.PreferredVoiceName,
            TimeZoneId: user.TimeZoneId,
            IsActive: user.IsActive,
            Roles: roleCodes,
            Permissions: permissionCodes,
            CreatedAtUtc: user.CreatedAtUtc,
            LastLoginAtUtc: user.LastLoginAtUtc
        );
}
