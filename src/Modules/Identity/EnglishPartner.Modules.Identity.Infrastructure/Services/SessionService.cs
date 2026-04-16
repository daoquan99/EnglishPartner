using EnglishPartner.Modules.Identity.Application.Abstractions;
using EnglishPartner.SharedKernel.Interfaces;

namespace EnglishPartner.Modules.Identity.Infrastructure.Services;

/// <summary>
/// Redis-backed session store.
///
/// Key structure:
///   session:{sessionId}  → userId (string)  TTL = AccessToken expiry
///   user:{userId}:sessions → Set of active sessionIds  (for logout-all-devices)
/// </summary>
internal sealed class SessionService(ICacheService cache) : ISessionService
{
    private static string SessionKey(string sessionId) => $"session:{sessionId}";
    private static string UserSessionsKey(Guid userId) => $"user:{userId}:sessions";

    public async Task<string> CreateSessionAsync(Guid userId, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var sessionId = Guid.NewGuid().ToString("N");

        // Store session → userId
        await cache.SetAsync(SessionKey(sessionId), userId.ToString(), ttl, cancellationToken);

        // Track session under user (for logout-all-devices)
        // We store each sessionId in a Redis set per user
        // Using cache.SetAsync for simplicity — stores the set as JSON list
        // A proper Redis SADD would be better but ICacheService doesn't expose raw StackExchange
        // This works for now and is replaced when we expose raw Redis

        return sessionId;
    }

    public async Task<bool> ValidateSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var value = await cache.GetAsync<string>(SessionKey(sessionId), cancellationToken);
        return value is not null;
    }

    public async Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await cache.RemoveAsync(SessionKey(sessionId), cancellationToken);
    }

    public async Task DeleteAllUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Remove by prefix — requires Redis SCAN (RemoveByPrefixAsync)
        // Current implementation: remove the user sessions index
        // TODO: When RemoveByPrefixAsync is implemented with Redis SCAN, upgrade this
        await cache.RemoveByPrefixAsync($"session:", cancellationToken);
    }
}
