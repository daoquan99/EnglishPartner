using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.SharedKernel.Application.Caching;

public interface ICacheableQuery<TResponse> : IQuery<TResponse>
{
    string CacheKey { get; }
    TimeSpan? Expiration { get; }
}
