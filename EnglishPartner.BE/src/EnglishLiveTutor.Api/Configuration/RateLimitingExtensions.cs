using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace EnglishLiveTutor.Api.Configuration;

public static class RateLimitingExtensions
{
    public const string AuthPolicyName = "auth";
    public const string AuthRefreshPolicyName = "auth-refresh";

    public static RateLimitPartition<string> GetAuthRateLimitPartition(
        HttpContext httpContext,
        IConfiguration configuration)
    {
        int authPermitLimit = configuration.GetValue<int?>("RateLimiting:Auth:PermitLimit") ?? 10;
        int authWindowSeconds = configuration.GetValue<int?>("RateLimiting:Auth:WindowSeconds") ?? 60;
        string ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ip,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authPermitLimit,
                Window = TimeSpan.FromSeconds(authWindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    }

    public static RateLimitPartition<string> GetAuthRefreshRateLimitPartition(
        HttpContext httpContext,
        IConfiguration configuration)
    {
        int authRefreshPermitLimit = configuration.GetValue<int?>("RateLimiting:AuthRefresh:PermitLimit") ?? 30;
        int authRefreshWindowSeconds = configuration.GetValue<int?>("RateLimiting:AuthRefresh:WindowSeconds") ?? 60;
        string ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ip,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authRefreshPermitLimit,
                Window = TimeSpan.FromSeconds(authRefreshWindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    }

    public static PartitionedRateLimiter<HttpContext> CreateAuthRateLimiter(IConfiguration configuration)
    {
        return PartitionedRateLimiter.Create<HttpContext, string>(ctx => GetAuthRateLimitPartition(ctx, configuration));
    }

    public static PartitionedRateLimiter<HttpContext> CreateAuthRefreshRateLimiter(IConfiguration configuration)
    {
        return PartitionedRateLimiter.Create<HttpContext, string>(ctx => GetAuthRefreshRateLimitPartition(ctx, configuration));
    }

    public static RateLimiterOptions AddConfiguredRateLimiters(
        this RateLimiterOptions options,
        IConfiguration configuration)
    {
        options.AddPolicy(AuthPolicyName, ctx => GetAuthRateLimitPartition(ctx, configuration));
        options.AddPolicy(AuthRefreshPolicyName, ctx => GetAuthRefreshRateLimitPartition(ctx, configuration));
        return options;
    }
}
