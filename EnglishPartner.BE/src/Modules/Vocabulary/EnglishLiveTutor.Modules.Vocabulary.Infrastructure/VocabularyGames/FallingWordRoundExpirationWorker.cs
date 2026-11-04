using EnglishLiveTutor.Application.Features.VocabularyGames.Commands.ExpireFallingWordRounds;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishLiveTutor.Infrastructure.VocabularyGames;

/// <summary>
/// Background worker that periodically triggers ExpireFallingWordRoundsCommand in Application,
/// keeping business orchestration, aggregate mutation, and persistence boundaries within Application.
/// </summary>
public sealed class FallingWordRoundExpirationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<FallingWordRoundExpirationWorker> _logger;
    private readonly TimeSpan _pollingInterval;

    public TimeSpan PollingInterval => _pollingInterval;

    public FallingWordRoundExpirationWorker(
        IServiceProvider serviceProvider,
        TimeProvider timeProvider,
        ILogger<FallingWordRoundExpirationWorker> logger,
        IOptions<VocabularyGamesOptions>? options = null,
        IConfiguration? configuration = null)
    {
        _serviceProvider = serviceProvider;
        _timeProvider = timeProvider;
        _logger = logger;

        int pollSeconds = VocabularyGamesOptions.DefaultExpirationPollSeconds;
        if (options?.Value != null)
        {
            pollSeconds = options.Value.ExpirationPollSeconds;
        }
        else if (configuration != null)
        {
            var configSec = configuration.GetValue<int?>("VocabularyGames:ExpirationPollSeconds");
            if (configSec.HasValue)
            {
                pollSeconds = configSec.Value;
            }
        }

        if (pollSeconds < VocabularyGamesOptions.MinExpirationPollSeconds || pollSeconds > VocabularyGamesOptions.MaxExpirationPollSeconds)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                pollSeconds,
                $"VocabularyGames:ExpirationPollSeconds must be between {VocabularyGamesOptions.MinExpirationPollSeconds} and {VocabularyGamesOptions.MaxExpirationPollSeconds} seconds.");
        }

        _pollingInterval = TimeSpan.FromSeconds(pollSeconds);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("FallingWordRoundExpirationWorker started with polling interval {PollingIntervalSeconds}s.", _pollingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredRoundsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi bất ngờ trong chu kỳ FallingWordRoundExpirationWorker.");
            }

            try
            {
                await Task.Delay(_pollingInterval, _timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("FallingWordRoundExpirationWorker stopped.");
    }

    public async Task<int> ProcessExpiredRoundsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        return await sender.Send(new ExpireFallingWordRoundsCommand(BatchSize: 50), ct);
    }
}
