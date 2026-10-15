namespace EnglishLiveTutor.Application.Common.Interfaces;

/// <summary>
/// Marker interface indicating that the query response can be cached.
/// </summary>
public interface ICacheableQuery
{
    string CacheKey { get; }
    TimeSpan? Expiration { get; }
}
