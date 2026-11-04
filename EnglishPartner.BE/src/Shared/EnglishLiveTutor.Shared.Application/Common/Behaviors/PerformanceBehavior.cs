using System.Diagnostics;
using EnglishLiveTutor.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EnglishLiveTutor.Application.Common.Behaviors;

public sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
    ICurrentUser? currentUser = null)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowRequestThresholdMs = 500;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        var response = await next(ct);

        stopwatch.Stop();
        var elapsedMs = stopwatch.ElapsedMilliseconds;

        if (elapsedMs > SlowRequestThresholdMs)
        {
            var requestName = typeof(TRequest).Name;
            var userId = currentUser?.UserId ?? "Anonymous";

            logger.LogWarning(
                "Long Running Request: {RequestName} ({ElapsedMilliseconds} ms) by user {UserId}",
                requestName,
                elapsedMs,
                userId);
        }

        return response;
    }
}
