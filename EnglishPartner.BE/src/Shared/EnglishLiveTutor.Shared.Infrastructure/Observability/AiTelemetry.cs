using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace EnglishLiveTutor.Infrastructure.Observability;

public static class AiTelemetry
{
    public const string SourceName = "EnglishLiveTutor.Ai";
    public const string MeterName = "EnglishLiveTutor.Ai";

    public static readonly ActivitySource Source = new(SourceName);
    public static readonly Meter Meter = new(MeterName);

    private static readonly Regex ModelCodeRegex = new("^[a-z0-9][a-z0-9.\\-]{0,63}$", RegexOptions.Compiled);

    // GenAI client operation duration histogram (seconds)
    private static readonly Histogram<double> OperationDuration = Meter.CreateHistogram<double>(
        name: "gen_ai.client.operation.duration",
        unit: "s",
        description: "Duration of GenAI client operations");

    // GenAI client token usage histogram ({token})
    private static readonly Histogram<long> TokenUsage = Meter.CreateHistogram<long>(
        name: "gen_ai.client.token.usage",
        unit: "{token}",
        description: "Number of tokens consumed by GenAI client operations");

    // Live tutor active sessions counter
    private static readonly UpDownCounter<long> ActiveSessions = Meter.CreateUpDownCounter<long>(
        name: "ept.livetutor.sessions.active",
        description: "Number of currently active Live Tutor sessions");

    // Live tutor session duration histogram (seconds)
    private static readonly Histogram<double> SessionDuration = Meter.CreateHistogram<double>(
        name: "ept.livetutor.session.duration",
        unit: "s",
        description: "Duration of completed Live Tutor sessions");

    public static string NormalizeModelCode(string? modelCode)
    {
        if (string.IsNullOrEmpty(modelCode))
            return "other";

        string cleaned = modelCode.StartsWith("models/", StringComparison.OrdinalIgnoreCase)
            ? modelCode["models/".Length..]
            : modelCode;

        cleaned = cleaned.Trim().ToLowerInvariant();

        return ModelCodeRegex.IsMatch(cleaned) ? cleaned : "other";
    }

    public static string NormalizeLiveRole(string? roleTag)
    {
        if (string.IsNullOrWhiteSpace(roleTag))
            return "other";

        string lower = roleTag.Trim().ToLowerInvariant();
        if (lower.Contains("tutor")) return "tutor";
        if (lower.Contains("transcribe")) return "transcribe";
        if (lower.Contains("translate")) return "translate";
        if (lower.Contains("tts")) return "tts";
        return "other";
    }

    public static void RecordLiveSessionStarted(string? role = null)
    {
        string normalizedRole = NormalizeLiveRole(role);
        ActiveSessions.Add(1, new KeyValuePair<string, object?>("ept.livetutor.role", normalizedRole));
    }

    public static void RecordLiveSessionStopped(
        double durationSeconds,
        long? inputTokens = null,
        long? outputTokens = null,
        string? role = null,
        string? model = null)
    {
        string normalizedRole = NormalizeLiveRole(role);
        string normalizedModel = NormalizeModelCode(model);

        ActiveSessions.Add(-1, new KeyValuePair<string, object?>("ept.livetutor.role", normalizedRole));
        if (durationSeconds >= 0)
        {
            SessionDuration.Record(durationSeconds,
                new KeyValuePair<string, object?>("ept.livetutor.role", normalizedRole));
        }

        if (inputTokens.HasValue && inputTokens.Value > 0)
        {
            TokenUsage.Record(inputTokens.Value,
                new KeyValuePair<string, object?>("gen_ai.provider.name", "gemini"),
                new KeyValuePair<string, object?>("gen_ai.operation.name", "live"),
                new KeyValuePair<string, object?>("gen_ai.request.model", normalizedModel),
                new KeyValuePair<string, object?>("ept.ai.feature", "livetutor.session"),
                new KeyValuePair<string, object?>("ept.livetutor.role", normalizedRole),
                new KeyValuePair<string, object?>("gen_ai.token.type", "input"));
        }

        if (outputTokens.HasValue && outputTokens.Value > 0)
        {
            TokenUsage.Record(outputTokens.Value,
                new KeyValuePair<string, object?>("gen_ai.provider.name", "gemini"),
                new KeyValuePair<string, object?>("gen_ai.operation.name", "live"),
                new KeyValuePair<string, object?>("gen_ai.request.model", normalizedModel),
                new KeyValuePair<string, object?>("ept.ai.feature", "livetutor.session"),
                new KeyValuePair<string, object?>("ept.livetutor.role", normalizedRole),
                new KeyValuePair<string, object?>("gen_ai.token.type", "output"));
        }
    }

    internal static (long? inputTokens, long? outputTokens) ExtractUsageTokens(JsonNode? usageOrRootNode)
    {
        if (usageOrRootNode is null)
            return (null, null);

        var usage = usageOrRootNode["usageMetadata"] ?? usageOrRootNode;

        long? prompt = ExtractTokenCount(usage["promptTokenCount"]);
        long? output = ExtractTokenCount(usage["responseTokenCount"])
            ?? ExtractTokenCount(usage["candidatesTokenCount"]);

        if (!output.HasValue && prompt.HasValue)
        {
            long? total = ExtractTokenCount(usage["totalTokenCount"]);
            if (total.HasValue && total.Value >= prompt.Value)
            {
                output = total.Value - prompt.Value;
            }
        }

        return (prompt, output);
    }

    internal static long? ExtractTokenCount(JsonNode? node)
    {
        if (node is null) return null;
        try
        {
            if (node is JsonValue val && val.TryGetValue<long>(out var count))
                return count;
            return node.GetValue<long>();
        }
        catch
        {
            return null;
        }
    }

    public static AiCallScope Start(
        string feature,
        string modelCode,
        string operationName = "generate_content",
        CancellationToken ct = default)
    {
        return new AiCallScope(feature, modelCode, operationName, ct);
    }

    public sealed class AiCallScope : IDisposable
    {
        private readonly string _feature;
        private readonly string _modelCode;
        private readonly string _operationName;
        private readonly CancellationToken _cancellationToken;
        private readonly long _startTimestamp;
        private readonly Activity? _activity;
        private int _disposed;
        private bool _succeeded;
        private string? _errorType;

        internal AiCallScope(string feature, string modelCode, string operationName, CancellationToken cancellationToken)
        {
            _feature = feature ?? string.Empty;
            _modelCode = NormalizeModelCode(modelCode);
            _operationName = operationName ?? "generate_content";
            _cancellationToken = cancellationToken;
            _startTimestamp = Stopwatch.GetTimestamp();

            _activity = Source.StartActivity(_operationName, ActivityKind.Client);
            if (_activity is not null)
            {
                _activity.SetTag("gen_ai.provider.name", "gemini");
                _activity.SetTag("gen_ai.operation.name", _operationName);
                _activity.SetTag("gen_ai.request.model", _modelCode);
                _activity.SetTag("ept.ai.feature", _feature);
            }
        }

        public Activity? Activity => _activity;

        public void Succeed()
        {
            if (_errorType is null)
            {
                _succeeded = true;
            }
        }

        public void RecordUsage(string? jsonResponse)
        {
            if (string.IsNullOrWhiteSpace(jsonResponse))
                return;

            try
            {
                var root = JsonNode.Parse(jsonResponse);
                RecordUsage(root);
            }
            catch
            {
                // Usage parse failure should not break caller flow
            }
        }

        public void RecordUsage(JsonNode? root)
        {
            if (root is null)
                return;

            try
            {
                var (promptTokens, candidatesTokens) = ExtractUsageTokens(root);

                if (promptTokens.HasValue && promptTokens.Value > 0)
                {
                    TokenUsage.Record(promptTokens.Value,
                        new KeyValuePair<string, object?>("gen_ai.provider.name", "gemini"),
                        new KeyValuePair<string, object?>("gen_ai.operation.name", _operationName),
                        new KeyValuePair<string, object?>("gen_ai.request.model", _modelCode),
                        new KeyValuePair<string, object?>("ept.ai.feature", _feature),
                        new KeyValuePair<string, object?>("gen_ai.token.type", "input"));

                    _activity?.SetTag("gen_ai.usage.input_tokens", promptTokens.Value);
                }

                if (candidatesTokens.HasValue && candidatesTokens.Value > 0)
                {
                    TokenUsage.Record(candidatesTokens.Value,
                        new KeyValuePair<string, object?>("gen_ai.provider.name", "gemini"),
                        new KeyValuePair<string, object?>("gen_ai.operation.name", _operationName),
                        new KeyValuePair<string, object?>("gen_ai.request.model", _modelCode),
                        new KeyValuePair<string, object?>("ept.ai.feature", _feature),
                        new KeyValuePair<string, object?>("gen_ai.token.type", "output"));

                    _activity?.SetTag("gen_ai.usage.output_tokens", candidatesTokens.Value);
                }
            }
            catch
            {
                // Silently swallow metric parse issues
            }
        }

        public void Fail(Exception? ex)
        {
            _succeeded = false;
            if (ex is null)
            {
                Fail("error");
                return;
            }

            if (ex is TimeoutException || ex.InnerException is TimeoutException)
            {
                Fail("timeout");
            }
            else if (ex is OperationCanceledException)
            {
                if (!_cancellationToken.IsCancellationRequested)
                {
                    Fail("timeout");
                }
                else
                {
                    Fail("canceled");
                }
            }
            else if (ex is HttpRequestException hre && hre.StatusCode.HasValue)
            {
                Fail($"http_{(int)hre.StatusCode.Value}");
            }
            else if (ex is JsonException)
            {
                Fail("invalid_response");
            }
            else
            {
                Fail("error");
            }
        }

        public void Fail(HttpStatusCode statusCode)
        {
            Fail($"http_{(int)statusCode}");
        }

        public void Fail(string errorType)
        {
            _succeeded = false;
            if (string.IsNullOrWhiteSpace(errorType))
                errorType = "error";

            _errorType = errorType;
            _activity?.SetTag("error.type", errorType);
            _activity?.SetStatus(ActivityStatusCode.Error, errorType);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;

            double durationSeconds = Stopwatch.GetElapsedTime(_startTimestamp).TotalSeconds;

            if (_succeeded && _errorType is null)
            {
                OperationDuration.Record(durationSeconds,
                    new KeyValuePair<string, object?>("gen_ai.provider.name", "gemini"),
                    new KeyValuePair<string, object?>("gen_ai.operation.name", _operationName),
                    new KeyValuePair<string, object?>("gen_ai.request.model", _modelCode),
                    new KeyValuePair<string, object?>("ept.ai.feature", _feature));
            }
            else
            {
                string errorType = _errorType
                    ?? (_cancellationToken.IsCancellationRequested ? "canceled" : "error");

                if (_activity is not null)
                {
                    _activity.SetTag("error.type", errorType);
                    _activity.SetStatus(ActivityStatusCode.Error, errorType);
                }

                OperationDuration.Record(durationSeconds,
                    new KeyValuePair<string, object?>("gen_ai.provider.name", "gemini"),
                    new KeyValuePair<string, object?>("gen_ai.operation.name", _operationName),
                    new KeyValuePair<string, object?>("gen_ai.request.model", _modelCode),
                    new KeyValuePair<string, object?>("ept.ai.feature", _feature),
                    new KeyValuePair<string, object?>("error.type", errorType));
            }

            _activity?.Dispose();
        }
    }
}
