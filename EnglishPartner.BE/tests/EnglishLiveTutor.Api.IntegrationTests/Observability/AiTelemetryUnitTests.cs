using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using EnglishLiveTutor.Infrastructure.Observability;
using Xunit;

namespace EnglishLiveTutor.Api.IntegrationTests.Observability;

[Collection("LiveTelemetry")]
public sealed class AiTelemetryUnitTests
{
    [Fact]
    public void SuccessWithUsageMetadata_RecordsDurationAndTokens()
    {
        var activities = new List<Activity>();
        using var activityListener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == AiTelemetry.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a => activities.Add(a)
        };
        ActivitySource.AddActivityListener(activityListener);

        var recordedMeasurements = new List<(string Name, object Value, KeyValuePair<string, object?>[] Tags)>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == AiTelemetry.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<double>((instrument, val, tags, state) =>
        {
            lock (recordedMeasurements)
            {
                recordedMeasurements.Add((instrument.Name, val, tags.ToArray()));
            }
        });
        meterListener.SetMeasurementEventCallback<long>((instrument, val, tags, state) =>
        {
            lock (recordedMeasurements)
            {
                recordedMeasurements.Add((instrument.Name, val, tags.ToArray()));
            }
        });
        meterListener.Start();

        using (var scope = AiTelemetry.Start("test.grammar", "gemini-test-model"))
        {
            string json = "{\"usageMetadata\":{\"promptTokenCount\":42,\"candidatesTokenCount\":84,\"totalTokenCount\":126}}";
            scope.RecordUsage(json);
            scope.Succeed();
        }

        meterListener.RecordObservableInstruments();

        // 1. Duration check
        var durationMeasurement = recordedMeasurements
            .Where(m => m.Name == "gen_ai.client.operation.duration")
            .FirstOrDefault(m => m.Tags.Any(t => t.Key == "ept.ai.feature" && (string?)t.Value == "test.grammar"));

        Assert.NotNull(durationMeasurement.Name);
        Assert.True((double)durationMeasurement.Value > 0);
        Assert.Contains(durationMeasurement.Tags, t => t.Key == "gen_ai.provider.name" && (string?)t.Value == "gemini");
        Assert.Contains(durationMeasurement.Tags, t => t.Key == "gen_ai.request.model" && (string?)t.Value == "gemini-test-model");
        Assert.DoesNotContain(durationMeasurement.Tags, t => t.Key == "error.type");

        // 2. Token usage check
        var tokenMeasurements = recordedMeasurements
            .Where(m => m.Name == "gen_ai.client.token.usage" &&
                        m.Tags.Any(t => t.Key == "ept.ai.feature" && (string?)t.Value == "test.grammar"))
            .ToList();

        Assert.Equal(2, tokenMeasurements.Count);
        var inputM = tokenMeasurements.First(m => m.Tags.Any(t => t.Key == "gen_ai.token.type" && (string?)t.Value == "input"));
        var outputM = tokenMeasurements.First(m => m.Tags.Any(t => t.Key == "gen_ai.token.type" && (string?)t.Value == "output"));

        Assert.Equal(42L, (long)inputM.Value);
        Assert.Equal(84L, (long)outputM.Value);

        // 3. Activity tags
        var act = activities.FirstOrDefault(a => a.GetTagItem("ept.ai.feature") as string == "test.grammar");
        Assert.NotNull(act);
        Assert.Equal("gemini", act.GetTagItem("gen_ai.provider.name"));
        Assert.Equal("gemini-test-model", act.GetTagItem("gen_ai.request.model"));
        Assert.Equal(42L, act.GetTagItem("gen_ai.usage.input_tokens"));
        Assert.Equal(84L, act.GetTagItem("gen_ai.usage.output_tokens"));
    }

    [Fact]
    public void SuccessWithoutUsageMetadata_RecordsDurationOnly()
    {
        var recordedMeasurements = new List<(string Name, object Value, KeyValuePair<string, object?>[] Tags)>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == AiTelemetry.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<double>((instrument, val, tags, state) =>
        {
            lock (recordedMeasurements)
            {
                recordedMeasurements.Add((instrument.Name, val, tags.ToArray()));
            }
        });
        meterListener.SetMeasurementEventCallback<long>((instrument, val, tags, state) =>
        {
            lock (recordedMeasurements)
            {
                recordedMeasurements.Add((instrument.Name, val, tags.ToArray()));
            }
        });
        meterListener.Start();

        using (var scope = AiTelemetry.Start("test.nousage", "gemini-flash"))
        {
            scope.RecordUsage("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"hello\"}]}}]}");
            scope.Succeed();
        }

        var durationM = recordedMeasurements
            .Where(m => m.Name == "gen_ai.client.operation.duration")
            .FirstOrDefault(m => m.Tags.Any(t => t.Key == "ept.ai.feature" && (string?)t.Value == "test.nousage"));
        Assert.NotNull(durationM.Name);

        var tokenM = recordedMeasurements
            .Where(m => m.Name == "gen_ai.client.token.usage" &&
                        m.Tags.Any(t => t.Key == "ept.ai.feature" && (string?)t.Value == "test.nousage"))
            .ToList();
        Assert.Empty(tokenM);
    }

    [Theory]
    [InlineData("timeout")]
    [InlineData("http_502")]
    [InlineData("invalid_response")]
    public void Fail_RecordsExpectedErrorType(string expectedType)
    {
        var recordedMeasurements = new List<(string Name, object Value, KeyValuePair<string, object?>[] Tags)>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == AiTelemetry.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<double>((instrument, val, tags, state) =>
        {
            lock (recordedMeasurements)
            {
                recordedMeasurements.Add((instrument.Name, val, tags.ToArray()));
            }
        });
        meterListener.Start();

        string feature = $"test.fail.{expectedType}";
        using (var scope = AiTelemetry.Start(feature, "gemini-flash"))
        {
            if (expectedType == "timeout")
            {
                scope.Fail(new TimeoutException("Timed out"));
            }
            else if (expectedType == "http_502")
            {
                scope.Fail(new HttpRequestException("Upstream error", null, HttpStatusCode.BadGateway));
            }
            else if (expectedType == "invalid_response")
            {
                scope.Fail(new System.Text.Json.JsonException("Malformed JSON"));
            }
        }

        var durationM = recordedMeasurements
            .Where(m => m.Name == "gen_ai.client.operation.duration")
            .FirstOrDefault(m => m.Tags.Any(t => t.Key == "ept.ai.feature" && (string?)t.Value == feature));

        Assert.NotNull(durationM.Name);
        Assert.Contains(durationM.Tags, t => t.Key == "error.type" && (string?)t.Value == expectedType);
    }

    [Fact]
    public void DoubleDispose_RecordsDurationOnlyOnce()
    {
        var recordedMeasurements = new List<(string Name, object Value, KeyValuePair<string, object?>[] Tags)>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == AiTelemetry.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<double>((instrument, val, tags, state) =>
        {
            lock (recordedMeasurements)
            {
                recordedMeasurements.Add((instrument.Name, val, tags.ToArray()));
            }
        });
        meterListener.Start();

        var scope = AiTelemetry.Start("test.doubledispose", "gemini-flash");
        scope.Succeed();
        scope.Dispose();
        scope.Dispose();

        var durationList = recordedMeasurements
            .Where(m => m.Name == "gen_ai.client.operation.duration" &&
                        m.Tags.Any(t => t.Key == "ept.ai.feature" && (string?)t.Value == "test.doubledispose"))
            .ToList();

        Assert.Single(durationList);
    }

    [Fact]
    public void DisposeWithoutSucceed_RecordsErrorByDefault()
    {
        var recordedMeasurements = new List<(string Name, object Value, KeyValuePair<string, object?>[] Tags)>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == AiTelemetry.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<double>((instrument, val, tags, state) =>
        {
            lock (recordedMeasurements)
            {
                recordedMeasurements.Add((instrument.Name, val, tags.ToArray()));
            }
        });
        meterListener.Start();

        using (var scope = AiTelemetry.Start("test.unhandled", "gemini-flash"))
        {
            // Neither Succeed() nor Fail() called - simulates exception thrown by caller or unhandled crash
        }

        var durationM = recordedMeasurements
            .Where(m => m.Name == "gen_ai.client.operation.duration")
            .FirstOrDefault(m => m.Tags.Any(t => t.Key == "ept.ai.feature" && (string?)t.Value == "test.unhandled"));

        Assert.NotNull(durationM.Name);
        Assert.Contains(durationM.Tags, t => t.Key == "error.type" && (string?)t.Value == "error");
    }

    [Fact]
    public void DisposeWithoutSucceed_WhenCancellationTokenCanceled_RecordsCanceled()
    {
        var recordedMeasurements = new List<(string Name, object Value, KeyValuePair<string, object?>[] Tags)>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == AiTelemetry.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<double>((instrument, val, tags, state) =>
        {
            lock (recordedMeasurements)
            {
                recordedMeasurements.Add((instrument.Name, val, tags.ToArray()));
            }
        });
        meterListener.Start();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        using (var scope = AiTelemetry.Start("test.canceled", "gemini-flash", ct: cts.Token))
        {
            // Neither Succeed() nor Fail() called, but token is canceled
        }

        var durationM = recordedMeasurements
            .Where(m => m.Name == "gen_ai.client.operation.duration")
            .FirstOrDefault(m => m.Tags.Any(t => t.Key == "ept.ai.feature" && (string?)t.Value == "test.canceled"));

        Assert.NotNull(durationM.Name);
        Assert.Contains(durationM.Tags, t => t.Key == "error.type" && (string?)t.Value == "canceled");
    }

    [Theory]
    [InlineData("gemini-2.0-flash", "gemini-2.0-flash")]
    [InlineData("models/gemini-3.8-live", "gemini-3.8-live")]
    [InlineData("gemini-3.5-transcribe-live", "gemini-3.5-transcribe-live")]
    [InlineData("gemini-1.5-pro-preview-0409", "gemini-1.5-pro-preview-0409")]
    [InlineData("", "other")]
    [InlineData(null, "other")]
    [InlineData("INVALID MODEL WITH SPACES", "other")]
    [InlineData("models/invalid/deep/nested", "other")]
    public void ModelNormalization_LimitsCardinality(string? input, string expected)
    {
        string actual = AiTelemetry.NormalizeModelCode(input);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("tutor", "tutor")]
    [InlineData("transcribe", "transcribe")]
    [InlineData("translate", "translate")]
    [InlineData("tts-synth", "tts")]
    [InlineData("tts", "tts")]
    [InlineData(null, "other")]
    [InlineData("", "other")]
    [InlineData("arbitrary-role", "other")]
    public void LiveRoleNormalization_MapsToFixedSet(string? input, string expected)
    {
        string actual = AiTelemetry.NormalizeLiveRole(input);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LiveSession_TracksRoleAndModelTags()
    {
        var recordedMeasurements = new List<(string Name, object Value, KeyValuePair<string, object?>[] Tags)>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == AiTelemetry.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<double>((instrument, val, tags, state) =>
        {
            lock (recordedMeasurements)
            {
                recordedMeasurements.Add((instrument.Name, val, tags.ToArray()));
            }
        });
        meterListener.SetMeasurementEventCallback<long>((instrument, val, tags, state) =>
        {
            lock (recordedMeasurements)
            {
                recordedMeasurements.Add((instrument.Name, val, tags.ToArray()));
            }
        });
        meterListener.Start();

        AiTelemetry.RecordLiveSessionStarted("tutor");
        AiTelemetry.RecordLiveSessionStopped(25.4, inputTokens: 500, outputTokens: 1000, role: "tutor", model: "models/gemini-3.8-live-unique");

        var activeCounters = recordedMeasurements.Where(m => m.Name == "ept.livetutor.sessions.active").ToList();
        Assert.Contains(activeCounters, m => (long)m.Value == 1 && m.Tags.Any(t => t.Key == "ept.livetutor.role" && (string?)t.Value == "tutor"));
        Assert.Contains(activeCounters, m => (long)m.Value == -1 && m.Tags.Any(t => t.Key == "ept.livetutor.role" && (string?)t.Value == "tutor"));

        var durationMeasurements = recordedMeasurements
            .Where(m => m.Name == "ept.livetutor.session.duration" &&
                        m.Tags.Any(t => t.Key == "ept.livetutor.role" && (string?)t.Value == "tutor") &&
                        (double)m.Value == 25.4)
            .ToList();
        Assert.NotEmpty(durationMeasurements);
        var durationM = durationMeasurements.First();
        Assert.Equal(25.4, (double)durationM.Value);
        Assert.Contains(durationM.Tags, t => t.Key == "ept.livetutor.role" && (string?)t.Value == "tutor");

        var liveTokens = recordedMeasurements
            .Where(m => m.Name == "gen_ai.client.token.usage" &&
                        m.Tags.Any(t => t.Key == "gen_ai.operation.name" && (string?)t.Value == "live") &&
                        m.Tags.Any(t => t.Key == "gen_ai.request.model" && (string?)t.Value == "gemini-3.8-live-unique"))
            .ToList();

        Assert.Equal(2, liveTokens.Count);
        foreach (var tok in liveTokens)
        {
            Assert.Contains(tok.Tags, t => t.Key == "ept.livetutor.role" && (string?)t.Value == "tutor");
            Assert.Contains(tok.Tags, t => t.Key == "gen_ai.request.model" && (string?)t.Value == "gemini-3.8-live-unique");
        }
    }

    [Fact]
    public async Task HttpClientTimeout_50ms_RecordsErrorTypeTimeout()
    {
        var recordedMeasurements = new List<(string Name, object Value, KeyValuePair<string, object?>[] Tags)>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == AiTelemetry.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<double>((instrument, val, tags, state) =>
        {
            lock (recordedMeasurements)
            {
                recordedMeasurements.Add((instrument.Name, val, tags.ToArray()));
            }
        });
        meterListener.Start();

        var handler = new DelayHttpMessageHandler(TimeSpan.FromSeconds(1));
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(50) };
        using var callerCts = new CancellationTokenSource();

        string feature = "test.timeout.http50ms";
        using (var scope = AiTelemetry.Start(feature, "gemini-flash-timeout", ct: callerCts.Token))
        {
            try
            {
                await client.GetAsync("http://example.com/ai", callerCts.Token);
                scope.Succeed();
            }
            catch (Exception ex)
            {
                scope.Fail(ex);
            }
        }

        var durationM = recordedMeasurements
            .Where(m => m.Name == "gen_ai.client.operation.duration")
            .FirstOrDefault(m => m.Tags.Any(t => t.Key == "ept.ai.feature" && (string?)t.Value == feature));

        Assert.NotNull(durationM.Name);
        Assert.Contains(durationM.Tags, t => t.Key == "error.type" && (string?)t.Value == "timeout");
    }

    [Fact]
    public async Task CallerCancellationTokenCanceled_RecordsErrorTypeCanceled()
    {
        var recordedMeasurements = new List<(string Name, object Value, KeyValuePair<string, object?>[] Tags)>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == AiTelemetry.MeterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<double>((instrument, val, tags, state) =>
        {
            lock (recordedMeasurements)
            {
                recordedMeasurements.Add((instrument.Name, val, tags.ToArray()));
            }
        });
        meterListener.Start();

        var handler = new DelayHttpMessageHandler(TimeSpan.FromSeconds(1));
        using var client = new HttpClient(handler);
        using var callerCts = new CancellationTokenSource();

        string feature = "test.canceled.caller";
        using (var scope = AiTelemetry.Start(feature, "gemini-flash-cancel", ct: callerCts.Token))
        {
            try
            {
                var task = client.GetAsync("http://example.com/ai", callerCts.Token);
                callerCts.Cancel();
                await task;
                scope.Succeed();
            }
            catch (Exception ex)
            {
                scope.Fail(ex);
            }
        }

        var durationM = recordedMeasurements
            .Where(m => m.Name == "gen_ai.client.operation.duration")
            .FirstOrDefault(m => m.Tags.Any(t => t.Key == "ept.ai.feature" && (string?)t.Value == feature));

        Assert.NotNull(durationM.Name);
        Assert.Contains(durationM.Tags, t => t.Key == "error.type" && (string?)t.Value == "canceled");
    }

    private sealed class DelayHttpMessageHandler : HttpMessageHandler
    {
        private readonly TimeSpan _delay;
        public DelayHttpMessageHandler(TimeSpan delay) => _delay = delay;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(_delay, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
