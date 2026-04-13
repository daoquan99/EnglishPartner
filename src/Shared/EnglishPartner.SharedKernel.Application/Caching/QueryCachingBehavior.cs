using EnglishPartner.SharedKernel.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EnglishPartner.SharedKernel.Application.Caching;

public sealed class QueryCachingBehavior<TRequest, TResponse>(
    ICacheService cacheService,
    ILogger<QueryCachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICacheableQuery<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var cacheKey = request.CacheKey;
        var expiration = request.Expiration;

        logger.LogDebug("Evaluating cache for query '{QueryType}' with key '{CacheKey}'", typeof(TRequest).Name, cacheKey);

        var cachedResponse = await cacheService.GetAsync<TResponse>(cacheKey, cancellationToken);
        if (cachedResponse is null)
        {
            logger.LogDebug("Cache miss for '{CacheKey}'", cacheKey);
            
            // Execute the actual handler
            var response = await next();

            // Only cache successful requests if using Results pattern
            if (response != null)
            {
                await cacheService.SetAsync(cacheKey, response, expiration, cancellationToken);
            }

            return response;
        }

        logger.LogDebug("Cache hit for '{CacheKey}'", cacheKey);
        return cachedResponse;
    }
}
