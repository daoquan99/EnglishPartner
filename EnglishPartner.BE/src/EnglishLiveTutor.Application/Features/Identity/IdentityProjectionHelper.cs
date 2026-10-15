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

    public static async Task<Dictionary<Guid, (List<string> RoleCodes, List<string> PermissionCodes)>> ResolveUsersRolesAndPermissionsBatchAsync(
        IAppDbContext db,
        IReadOnlyCollection<Guid> userIds,
        CancellationToken ct = default)
    {
        var result = userIds.ToDictionary(id => id, _ => (new List<string>(), new List<string>()));
        if (userIds.Count == 0) return result;

        var userRoles = await (
            from ur in db.UserRoles.AsNoTracking()
            join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where userIds.Contains(ur.UserId) && r.IsActive
            select new { ur.UserId, r.Id, r.Code }
        ).ToListAsync(ct);

        if (userRoles.Count == 0) return result;

        var activeRoleIds = userRoles.Select(ur => ur.Id).Distinct().ToList();

        var rolePermissions = await (
            from rp in db.RolePermissions.AsNoTracking()
            join p in db.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
            where activeRoleIds.Contains(rp.RoleId)
            select new { rp.RoleId, p.Code }
        ).ToListAsync(ct);

        var rolePermissionMap = rolePermissions
            .GroupBy(rp => rp.RoleId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Code).ToHashSet());

        var userRoleGroups = userRoles.GroupBy(ur => ur.UserId);
        foreach (var ug in userRoleGroups)
        {
            var roleCodes = ug.Select(x => x.Code).Distinct().OrderBy(c => c).ToList();
            var permCodes = ug
                .Where(x => rolePermissionMap.ContainsKey(x.Id))
                .SelectMany(x => rolePermissionMap[x.Id])
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            result[ug.Key] = (roleCodes, permCodes);
        }

        return result;
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
