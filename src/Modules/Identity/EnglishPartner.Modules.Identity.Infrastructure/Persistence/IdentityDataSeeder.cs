using EnglishPartner.Modules.Identity.Domain.Roles;
using EnglishPartner.Modules.Identity.Domain.Roles.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Identity.Infrastructure.Persistence;

/// <summary>
/// Seeds default roles, permissions, and role-permission assignments.
/// All GUIDs are deterministic so migrations are stable across machines.
///
/// Learner role: role claim only in JWT — no permission claims.
/// Admin role: role claim + all admin permission claims in JWT.
/// </summary>
internal static class IdentityDataSeeder
{
    // ── Roles ──────────────────────────────────────────────────────────────────
    internal static readonly Guid LearnerRoleId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    internal static readonly Guid AdminRoleId   = Guid.Parse("00000000-0000-0000-0000-000000000002");

    // ── Permission GUIDs (deterministic — indexed, stable across runs) ─────────
    // Format: 00000000-0000-0000-0001-XXXXXXXXXXXX
    private static Guid PermId(int n) => Guid.Parse($"00000000-0000-0000-0001-{n:D12}");

    private static readonly string[] AllAdminPermCodes = Permissions.AdminPermissions;

    /// <summary>Map permission code → stable GUID (used by AdminUserSeeder).</summary>
    internal static readonly IReadOnlyDictionary<string, Guid> PermissionIds =
        AllAdminPermCodes
            .Select((code, i) => (code, id: PermId(i + 1)))
            .ToDictionary(x => x.code, x => x.id);

    private static string ModuleOf(string code) => code.Split('.')[0];

    public static void SeedRoles(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // ── Roles ──────────────────────────────────────────────────────────────
        modelBuilder.Entity<Role>().HasData(
            SeedRole(LearnerRoleId, "Learner", "Default role for all learners", seedDate),
            SeedRole(AdminRoleId,   "Admin",   "System administrator",          seedDate));

        // ── Permissions (admin-level only) ─────────────────────────────────────
        var permissionRows = AllAdminPermCodes.Select(code => new
        {
            Id          = PermissionIds[code],
            Code        = code,
            Name        = FormatName(code),
            Module      = ModuleOf(code),
            Description = (string?)null,
            CreatedAt   = seedDate,
        }).ToArray();

        modelBuilder.Entity<Permission>().HasData(permissionRows);

        // ── Assign ALL permissions to Admin role only ──────────────────────────
        // (Learner role gets no permissions — user endpoints use RequireAuthorization() only)
        var rpRows = AllAdminPermCodes
            .Select((code, i) => new
            {
                Id           = Guid.Parse($"00000000-0000-0000-0003-{i + 1:D12}"),
                RoleId       = AdminRoleId,
                PermissionId = PermissionIds[code],
                CreatedAt    = seedDate,
            })
            .ToArray<object>();

        modelBuilder.Entity<RolePermission>().HasData(rpRows);
    }

    // ──────────────────────────────────────────────────────────────────────────

    private static object SeedRole(Guid id, string name, string description, DateTime seedDate) => new
    {
        Id          = id,
        Name        = name,
        Description = description,
        IsActive    = true,
        IsDeleted   = false,
        CreatedAt   = seedDate,
    };

    private static string FormatName(string code)
    {
        var parts = code.Split('.');
        return parts.Length >= 3
            ? $"{Capitalize(parts[2])} {Capitalize(parts[1])}"
            : code;
    }

    private static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];
}
