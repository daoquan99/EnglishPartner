using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Persistence;

/// <summary>
/// Tracks schema migration and seed status separately so readiness endpoints do not report
/// ready when database migration failed.
/// </summary>
public sealed class DatabaseReadinessState
{
    private readonly IDbContextFactory<AppDbContext>? _dbFactory;

    public bool IsMigrationCompleted { get; private set; }
    public bool IsSeedCompleted { get; private set; }
    public string? LastMigrationError { get; private set; }
    public string? LastSeedError { get; private set; }
    public DateTimeOffset? LastCheckedAtUtc { get; private set; }

    public DatabaseReadinessState(IDbContextFactory<AppDbContext>? dbFactory = null)
    {
        _dbFactory = dbFactory;
    }

    public void MarkMigrationSucceeded()
    {
        IsMigrationCompleted = true;
        LastMigrationError = null;
        LastCheckedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkMigrationFailed(string errorMessage)
    {
        IsMigrationCompleted = false;
        LastMigrationError = errorMessage;
        LastCheckedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkSeedSucceeded()
    {
        IsSeedCompleted = true;
        LastSeedError = null;
        LastCheckedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkSeedFailed(string errorMessage)
    {
        IsSeedCompleted = false;
        LastSeedError = errorMessage;
        LastCheckedAtUtc = DateTimeOffset.UtcNow;
    }

    public async Task<(bool IsReady, string Status, string? Error)> CheckReadinessAsync(CancellationToken ct = default)
    {
        if (!IsMigrationCompleted)
        {
            return (false, "degraded", LastMigrationError ?? "Database schema migration has not completed.");
        }

        if (_dbFactory is not null)
        {
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync(ct);
                bool canConnect = await db.Database.CanConnectAsync(ct);
                if (!canConnect)
                {
                    return (false, "unhealthy", "Cannot connect to SQL Server database.");
                }
            }
            catch (Exception ex)
            {
                return (false, "unhealthy", ex.Message);
            }
        }

        return (true, IsSeedCompleted ? "ready" : "ready_without_optional_seed", LastSeedError);
    }
}
