using EnglishPartner.Modules.Identity.Infrastructure.Persistence;
using EnglishPartner.Modules.Learning.Infrastructure.Persistence;
using EnglishPartner.Modules.Notifications.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using EnglishPartner.SharedKernel.Application.Caching;

namespace EnglishPartner.Api.Endpoints;

/// <summary>
/// Admin dashboard endpoint — cross-module aggregate query.
/// Lives in the API host (composition root) because it intentionally queries
/// multiple module DbContexts. This is the ONLY file in the host allowed to do so.
/// </summary>
public static class AdminDashboardEndpoints
{
    public static void MapAdminDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("api/v1/admin/dashboard", async (
            IdentityDbContext identityDb,
            LearningDbContext learningDb,
            NotificationDbContext notifDb,
            IDistributedCache cache,
            TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            // Serve from cache if fresh (60s TTL) — admin refreshes shouldn't hammer 7 COUNT queries
            var cached = await cache.GetStringAsync(CacheKeys.Admin.DashboardStats, cancellationToken);
            if (cached is not null)
                return Results.Ok(JsonSerializer.Deserialize<object>(cached));

            var now             = timeProvider.GetUtcNow().UtcDateTime;
            var sevenDaysAgo    = now.AddDays(-7);
            var completedStatus = EnglishPartner.Modules.Learning.Domain.StudySessions.SessionStatus.Completed;

            // Run all queries in parallel — each uses a separate DbContext instance (scoped), safe to parallelize
            var totalUsersTask        = identityDb.Users.CountAsync(cancellationToken);
            var activeUsersTask       = identityDb.Users.CountAsync(u => u.UpdatedAt >= sevenDaysAgo, cancellationToken);
            var newRegistrationsTask  = identityDb.Users.CountAsync(u => u.CreatedAt >= sevenDaysAgo, cancellationToken);
            var totalSessionsTask     = learningDb.StudySessions.CountAsync(cancellationToken);
            var completedSessionsTask = learningDb.StudySessions.CountAsync(s => s.Status == completedStatus, cancellationToken);
            var avgScoreTask          = learningDb.StudySessions
                .Where(s => s.Status == completedStatus)
                .AverageAsync(s => (double?)s.Score, cancellationToken);
            var totalNotificationsTask = notifDb.Notifications.CountAsync(cancellationToken);

            await Task.WhenAll(
                totalUsersTask, activeUsersTask, newRegistrationsTask,
                totalSessionsTask, completedSessionsTask, avgScoreTask,
                totalNotificationsTask);

            var result = new
            {
                totalUsers                = totalUsersTask.Result,
                activeUsersLast7Days      = activeUsersTask.Result,
                newRegistrationsLast7Days = newRegistrationsTask.Result,
                totalSessions             = totalSessionsTask.Result,
                totalSessionsCompleted    = completedSessionsTask.Result,
                averageSessionScore       = Math.Round(avgScoreTask.Result ?? 0, 1),
                totalNotificationsSent    = totalNotificationsTask.Result,
                generatedAt               = now,
            };

            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheTtl.Admin.DashboardStats
            };
            await cache.SetStringAsync(CacheKeys.Admin.DashboardStats, JsonSerializer.Serialize(result), cacheOptions, cancellationToken);

            return Results.Ok(result);
        })
        .RequireAuthorization("Admin")
        .WithTags("Admin — Dashboard");
    }
}
