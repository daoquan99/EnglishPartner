using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.Identity.Contracts;
using EnglishLiveTutor.Domain.Identity.Users;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Identity.Mapping;

public static class IdentityProjectionHelper
{
    public static async Task<(List<string> RoleCodes, List<string> PermissionCodes)> ResolveUserRolesAndPermissionsAsync(
        IAppReadDbContext db,
        Guid userId,
        CancellationToken ct = default)
    {
        var batch = await ResolveRolesAndPermissionsForUsersAsync(db, new[] { userId }, ct);
        return batch.TryGetValue(userId, out var res)
            ? res
            : (new List<string>(), new List<string>());
    }

    public static async Task<Dictionary<Guid, (List<string> RoleCodes, List<string> PermissionCodes)>> ResolveRolesAndPermissionsForUsersAsync(
        IAppReadDbContext db,
        IReadOnlyCollection<Guid> userIds,
        CancellationToken ct = default)
    {
        var result = new Dictionary<Guid, (List<string> RoleCodes, List<string> PermissionCodes)>();
        if (userIds.Count == 0)
        {
            return result;
        }

        var distinctUserIds = userIds.Distinct().ToList();
        foreach (var id in distinctUserIds)
        {
            result[id] = (new List<string>(), new List<string>());
        }

        // Only consider non-deleted active users' roles
        var userRoles = await db.UserRoles
            .AsNoTracking()
            .Where(ur => distinctUserIds.Contains(ur.UserId) && db.Users.Any(u => u.Id == ur.UserId))
            .Select(ur => new { ur.UserId, ur.RoleId })
            .ToListAsync(ct);

        if (userRoles.Count == 0)
        {
            return result;
        }

        var allRoleIds = userRoles.Select(ur => ur.RoleId).Distinct().ToList();
        var activeRoles = await db.Roles
            .AsNoTracking()
            .Where(r => allRoleIds.Contains(r.Id) && r.IsActive)
            .Select(r => new { r.Id, r.Code })
            .ToListAsync(ct);

        var activeRolesById = activeRoles.ToDictionary(r => r.Id, r => r.Code);
        if (activeRolesById.Count == 0)
        {
            return result;
        }

        var activeRoleIds = activeRolesById.Keys.ToList();
        var rolePermissions = await db.RolePermissions
            .AsNoTracking()
            .Where(rp => activeRoleIds.Contains(rp.RoleId))
            .Select(rp => new { rp.RoleId, rp.PermissionId })
            .ToListAsync(ct);

        var allPermIds = rolePermissions.Select(rp => rp.PermissionId).Distinct().ToList();
        var permsById = allPermIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await db.Permissions
                .AsNoTracking()
                .Where(p => allPermIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Code, ct);

        var permsByRoleId = rolePermissions
            .GroupBy(rp => rp.RoleId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => permsById.TryGetValue(x.PermissionId, out var code) ? code : null)
                    .Where(c => c is not null)
                    .Select(c => c!)
                    .Distinct()
                    .ToList());

        foreach (var group in userRoles.GroupBy(ur => ur.UserId))
        {
            var uRoleCodes = new List<string>();
            var uPermCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var ur in group)
            {
                if (activeRolesById.TryGetValue(ur.RoleId, out var rCode))
                {
                    uRoleCodes.Add(rCode);
                    if (permsByRoleId.TryGetValue(ur.RoleId, out var pCodes))
                    {
                        foreach (var pCode in pCodes)
                        {
                            uPermCodes.Add(pCode);
                        }
                    }
                }
            }

            result[group.Key] = (
                uRoleCodes.Distinct().OrderBy(c => c).ToList(),
                uPermCodes.OrderBy(c => c).ToList()
            );
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
