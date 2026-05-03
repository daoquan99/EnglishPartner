using EnglishPartner.Modules.AI.Infrastructure.Persistence;
using EnglishPartner.Modules.Curriculum.Infrastructure.Persistence;
using EnglishPartner.Modules.Identity.Infrastructure.Persistence;
using EnglishPartner.Modules.Learning.Infrastructure.Persistence;
using EnglishPartner.Modules.Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace EnglishPartner.Api.Extensions;

internal static class DatabaseExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return;

        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var initLogger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            var identityDb = services.GetRequiredService<IdentityDbContext>();
            await identityDb.Database.MigrateAsync();

            var aiDb = services.GetRequiredService<AiDbContext>();
            await aiDb.Database.MigrateAsync();

            var curriculumDb = services.GetRequiredService<CurriculumDbContext>();
            await curriculumDb.Database.MigrateAsync();

            var learningDb = services.GetRequiredService<LearningDbContext>();
            await learningDb.Database.MigrateAsync();

            var notificationDb = services.GetRequiredService<NotificationDbContext>();
            await notificationDb.Database.MigrateAsync();

            initLogger.LogInformation("Database migrated successfully.");

            // ⭐ Seed admin user (idempotent — skips if already exists)
            var adminSeeder = services.GetRequiredService<EnglishPartner.Modules.Identity.Infrastructure.Services.AdminUserSeeder>();
            await adminSeeder.SeedAsync(services.GetRequiredService<IConfiguration>());
        }
        catch (Exception ex)
        {
            initLogger.LogError(ex, "An error occurred while migrating the database.");

            // In production, a failed migration should abort startup to avoid running
            // against an inconsistent schema. In development, we allow lenient startup.
            if (!app.Environment.IsDevelopment())
                throw;
        }
    }
}
