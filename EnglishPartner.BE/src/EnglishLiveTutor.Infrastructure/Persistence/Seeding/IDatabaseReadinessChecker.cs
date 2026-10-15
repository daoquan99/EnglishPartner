using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Persistence.Seeding;

public sealed record DatabaseReadinessStatus(
    bool IsReady,
    bool CanConnect,
    int PendingMigrationCount,
    string? ErrorMessage = null
);

public interface IDatabaseReadinessChecker
{
    Task<DatabaseReadinessStatus> CheckReadinessAsync(CancellationToken ct = default);
}

public sealed class DatabaseReadinessChecker : IDatabaseReadinessChecker
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public DatabaseReadinessChecker(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<DatabaseReadinessStatus> CheckReadinessAsync(CancellationToken ct = default)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            bool canConnect = await db.Database.CanConnectAsync(ct);
            if (!canConnect)
            {
                return new DatabaseReadinessStatus(false, false, -1, "Database connection failed.");
            }

            if (db.Database.IsRelational())
            {
                var pending = await db.Database.GetPendingMigrationsAsync(ct);
                int pendingCount = pending.Count();
                return new DatabaseReadinessStatus(
                    IsReady: pendingCount == 0,
                    CanConnect: true,
                    PendingMigrationCount: pendingCount,
                    ErrorMessage: pendingCount == 0 ? null : $"Database has {pendingCount} pending migration(s).");
            }

            return new DatabaseReadinessStatus(true, true, 0);
        }
        catch (Exception ex)
        {
            return new DatabaseReadinessStatus(false, false, -1, ex.Message);
        }
    }
}
