using EnglishPartner.Modules.Identity.Application.Abstractions;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Application.Caching;

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
    private static string SessionKey(string sessionId) => CacheKeys.Identity.Session(sessionId);
    private static string UserSessionsKey(Guid userId) => CacheKeys.Identity.UserSessions(userId);

    public async Task<string> CreateSessionAsync(Guid userId, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        await cache.SetAsync(SessionKey(sessionId), userId.ToString(), ttl, cancellationToken);
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
        await cache.RemoveByPrefixAsync("session:", cancellationToken);
    }
}
