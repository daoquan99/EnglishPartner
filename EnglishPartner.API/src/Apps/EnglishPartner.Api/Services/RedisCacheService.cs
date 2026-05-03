using EnglishPartner.SharedKernel.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;

namespace EnglishPartner.Api.Services;

/// <summary>
/// Redis-backed cache service implementation.
/// </summary>
internal sealed class RedisCacheService(
    IDistributedCache cache,
    IConnectionMultiplexer redis,
    Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions> redisOptions,
    ILogger<RedisCacheService> logger) : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        logger.LogInformation("Getting cache key: {Key}", key);
        var data = await cache.GetStringAsync(key, cancellationToken);
        return data is null ? default : JsonSerializer.Deserialize<T>(data, JsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry, CancellationToken cancellationToken)
    {
        logger.LogInformation("Setting cache key: {Key} with expiry: {Expiry}", key, expiry);
        var options = new DistributedCacheEntryOptions();
        if (expiry.HasValue)
            options.AbsoluteExpirationRelativeToNow = expiry;

        var data = JsonSerializer.Serialize(value, JsonOptions);
        await cache.SetStringAsync(key, data, options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        logger.LogInformation("Removing cache key: {Key}", key);
        await cache.RemoveAsync(key, cancellationToken);
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken)
    {
        logger.LogInformation("Removing cache keys with prefix: {Prefix}", prefix);

        var instanceName = redisOptions.Value.InstanceName ?? string.Empty;
        var endpoints = redis.GetEndPoints();
        foreach (var endpoint in endpoints)
        {
            var server = redis.GetServer(endpoint);
            // Append InstanceName to search the exact key namespace created by IDistributedCache
            var keys = server.Keys(pattern: $"{instanceName}{prefix}*").ToArray();
            if (keys.Length > 0)
            {
                var db = redis.GetDatabase();
                await db.KeyDeleteAsync(keys);
                logger.LogInformation("Removed {Count} cache keys with prefix: {Prefix}", keys.Length, prefix);
            }
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry, CancellationToken cancellationToken)
    {
        logger.LogInformation("Attempting to get or set cache key: {Key}", key);
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached is not null)
            return cached;

        var value = await factory();
        await SetAsync(key, value, expiry, cancellationToken);
        return value;
    }
}
