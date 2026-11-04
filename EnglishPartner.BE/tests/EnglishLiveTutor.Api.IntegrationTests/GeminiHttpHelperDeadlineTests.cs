using System.Diagnostics;
using System.Net;
using EnglishLiveTutor.Infrastructure.Ai;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public sealed class GeminiHttpHelperDeadlineTests
{
    private sealed class AlwaysUnavailableHandler : HttpMessageHandler
    {
        public int Calls;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Calls);
            var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(1));
            return Task.FromResult(response);
        }
    }

    [Fact]
    public async Task PostJsonWithRetryAsync_StopsRetryingWhenHttpClientTimeoutElapses()
    {
        // 5 retries x 1s Retry-After would take ~5s; HttpClient.Timeout (Ai:*:TimeoutSeconds) caps the whole call.
        var handler = new AlwaysUnavailableHandler();
        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(1500) };
        var stopwatch = Stopwatch.StartNew();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => GeminiHttpHelper.PostJsonWithRetryAsync(
            http, "https://example.invalid/v1", new { }, "key", maxRetries: 5, NullLogger.Instance, CancellationToken.None));

        // Without the overall deadline all 6 attempts (1 + 5 retries) would run. Wall-clock time is only a loose
        // sanity bound because a loaded CI machine can delay timer continuations.
        Assert.InRange(handler.Calls, 1, 3);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(30), $"took {stopwatch.Elapsed}");
    }
}
