using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Persistence;

/// <summary>
/// Builds the EF model of every module DbContext and opens one pooled SQL connection right after
/// startup, so the first learner request does not pay for model building. Runs in the background
/// and never blocks or fails startup.
/// </summary>
public sealed class DbContextWarmupService(
    IServiceScopeFactory scopeFactory,
    ILogger<DbContextWarmupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var contexts = scope.ServiceProvider.GetServices<IUnitOfWorkParticipant>()
                .Select(p => p.DbContext)
                .ToList();

            foreach (var context in contexts)
            {
                stoppingToken.ThrowIfCancellationRequested();
                _ = context.Model; // builds and caches the model for the scoped options configuration
            }

            if (contexts.Count > 0 && contexts[0].Database.IsRelational())
            {
                // All scoped contexts share one connection; opening it fills the connection pool.
                await contexts[0].Database.OpenConnectionAsync(stoppingToken);
                await contexts[0].Database.CloseConnectionAsync();
            }

            logger.LogInformation("Warmed up {Count} DbContext models in {ElapsedMs} ms.", contexts.Count, stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            // Warm-up is an optimisation only; the first real request will build what is missing.
            logger.LogWarning(ex, "DbContext warm-up failed after {ElapsedMs} ms.", stopwatch.ElapsedMilliseconds);
        }
    }
}
