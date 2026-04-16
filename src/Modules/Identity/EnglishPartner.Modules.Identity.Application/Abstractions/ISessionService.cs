namespace EnglishPartner.Modules.Identity.Application.Abstractions;

/// <summary>
/// Manages server-side sessions in Redis.
/// Each JWT contains a 'sid' claim that maps to an entry here.
/// Deleting the session invalidates the JWT immediately (even before expiry).
/// </summary>
public interface ISessionService
{
    /// <summary>Creates a new session. Returns the sessionId to embed in the JWT.</summary>
    Task<string> CreateSessionAsync(Guid userId, TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>Returns true if the session exists (not deleted/expired).</summary>
    Task<bool> ValidateSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>Deletes a single session (logout current device).</summary>
    Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>Deletes ALL sessions for a user (logout all devices).</summary>
    Task DeleteAllUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
}
