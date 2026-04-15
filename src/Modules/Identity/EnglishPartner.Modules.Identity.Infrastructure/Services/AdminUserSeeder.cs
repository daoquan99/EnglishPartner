using EnglishPartner.Modules.Identity.Application.Abstractions;
using EnglishPartner.Modules.Identity.Application.Settings;
using EnglishPartner.Modules.Identity.Domain.Roles.Repositories;
using EnglishPartner.Modules.Identity.Domain.Users.Repositories;
using EnglishPartner.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnglishPartner.Modules.Identity.Infrastructure.Services;

/// <summary>
/// Seeds a default admin user on first startup if one does not already exist.
/// Called from Program.cs after all migrations have run.
/// Credentials come from appsettings.Development.json "Admin" section.
/// </summary>
public sealed class AdminUserSeeder(
    IdentityDbContext db,
    IPasswordHasher passwordHasher,
    ILogger<AdminUserSeeder> logger)
{
    private static readonly Guid AdminUserId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid AdminRoleId = IdentityDataSeeder.AdminRoleId;

    public async Task SeedAsync(IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        // Already seeded — idempotent
        if (await db.Users.AnyAsync(u => u.Id == AdminUserId, cancellationToken))
        {
            logger.LogDebug("Admin user already exists — skipping seed.");
            return;
        }

        var settings = configuration.GetSection(AdminSettings.SectionName).Get<AdminSettings>()
                       ?? new AdminSettings();

        if (string.IsNullOrWhiteSpace(settings.Password))
        {
            logger.LogWarning(
                "Admin password not configured in appsettings. Skipping admin user seed. " +
                "Set 'Admin:Password' in appsettings.Development.json.");
            return;
        }

        var passwordHash = passwordHasher.Hash(settings.Password);
        var adminEmail   = settings.Email.ToLowerInvariant();
        var displayName  = settings.DisplayName;
        var adminId      = AdminUserId;
        var roleId       = AdminRoleId;
        var rpId         = Guid.NewGuid();

        // Use ExecuteSqlAsync (parameterized) to avoid EF1002 SQL injection warning
        await db.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO identity.users ("Id","Email","PasswordHash","DisplayName","IsOnboardingCompleted","IsActive","IsDeleted","CreatedAt")
            VALUES ({adminId}, {adminEmail}, {passwordHash}, {displayName}, true, true, false, NOW())
            ON CONFLICT ("Id") DO NOTHING;
            """,
            cancellationToken);

        await db.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO identity.user_roles ("Id","UserId","RoleId","CreatedAt")
            VALUES ({rpId}, {adminId}, {roleId}, NOW())
            ON CONFLICT DO NOTHING;
            """,
            cancellationToken);

        logger.LogInformation("Admin user seeded: {Email}", settings.Email);
    }
}
