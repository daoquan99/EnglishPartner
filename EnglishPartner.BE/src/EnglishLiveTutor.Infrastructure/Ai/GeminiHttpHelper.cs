using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace EnglishLiveTutor.Infrastructure.Ai;

public static class GeminiHttpHelper
{
    public static async Task<(bool Success, HttpStatusCode StatusCode, string Body)> PostJsonWithRetryAsync(
        HttpClient httpClient,
        string url,
        object payload,
        string apiKey,
        int maxRetries,
        ILogger logger,
        CancellationToken ct)
    {
        int retries = Math.Clamp(maxRetries, 0, 5);

        for (int attempt = 0; attempt <= retries; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = JsonContent.Create(payload);
            request.Headers.Add("x-goog-api-key", apiKey.Trim());

            try
            {
                using var resp = await httpClient.SendAsync(request, ct);
                string body = await resp.Content.ReadAsStringAsync(ct);

                if (resp.IsSuccessStatusCode)
                {
                    return (true, resp.StatusCode, body);
                }

                int code = (int)resp.StatusCode;
                // Non-retriable client errors
                if (code is 400 or 401 or 403 or 404)
                {
                    logger.LogWarning("Gemini API call failed with non-retriable status {StatusCode}.", code);
                    return (false, resp.StatusCode, body);
                }

                // Transient retriable errors: 408, 429, 500, 502, 503, 504
                if (attempt < retries && IsRetriableStatusCode(resp.StatusCode))
                {
                    var delay = GetRetryDelay(resp.Headers, attempt);
                    logger.LogWarning("Gemini API call returned {StatusCode}. Retrying in {DelayMs}ms (attempt {Attempt}/{MaxRetries}).",
                        code, (int)delay.TotalMilliseconds, attempt + 1, retries);

                    await Task.Delay(delay, ct);
                    continue;
                }

                return (false, resp.StatusCode, body);
            }
            catch (HttpRequestException ex) when (attempt < retries && !ct.IsCancellationRequested)
            {
                var delay = TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt) + Random.Shared.Next(50, 150));
                logger.LogWarning(ex, "Network error calling Gemini API. Retrying in {DelayMs}ms (attempt {Attempt}/{MaxRetries}).",
                    (int)delay.TotalMilliseconds, attempt + 1, retries);

                await Task.Delay(delay, ct);
            }
            catch (TimeoutException ex) when (attempt < retries && !ct.IsCancellationRequested)
            {
                var delay = TimeSpan.FromMilliseconds(500 * Math.Pow(2, attempt) + Random.Shared.Next(50, 150));
                logger.LogWarning(ex, "Timeout calling Gemini API. Retrying in {DelayMs}ms (attempt {Attempt}/{MaxRetries}).",
                    (int)delay.TotalMilliseconds, attempt + 1, retries);

                await Task.Delay(delay, ct);
            }
        }

        return (false, HttpStatusCode.ServiceUnavailable, "Max retries reached calling Gemini API.");
    }

    private static bool IsRetriableStatusCode(HttpStatusCode code)
    {
        int statusCode = (int)code;
        return statusCode is 408 or 429 or 500 or 502 or 503 or 504;
    }

    private static TimeSpan GetRetryDelay(HttpResponseHeaders headers, int attempt)
    {
        if (headers.RetryAfter != null)
        {
            if (headers.RetryAfter.Delta.HasValue)
            {
                var delta = headers.RetryAfter.Delta.Value;
                if (delta > TimeSpan.Zero && delta <= TimeSpan.FromSeconds(30))
                {
                    return delta;
                }
            }
            else if (headers.RetryAfter.Date.HasValue)
            {
                var diff = headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow;
                if (diff > TimeSpan.Zero && diff <= TimeSpan.FromSeconds(30))
                {
                    return diff;
                }
            }
        }

        // Exponential backoff + jitter
        double baseMs = 500 * Math.Pow(2, attempt);
        int jitter = Random.Shared.Next(50, 200);
        return TimeSpan.FromMilliseconds(Math.Min(10000, baseMs + jitter));
    }
}
