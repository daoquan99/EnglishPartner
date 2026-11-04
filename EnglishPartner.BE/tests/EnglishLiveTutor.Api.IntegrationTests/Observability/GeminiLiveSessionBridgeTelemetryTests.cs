using System.Diagnostics.Metrics;
using EnglishLiveTutor.Infrastructure.Ai;
using EnglishLiveTutor.Infrastructure.Observability;
using Xunit;

namespace EnglishLiveTutor.Api.IntegrationTests.Observability;

[Collection("LiveTelemetry")]
public sealed class GeminiLiveSessionBridgeTelemetryTests
{
    [Fact]
    public async Task ConnectAsync_WhenTokenCanceled_ThrowsOperationCanceledException()
    {
        var bridge = new GeminiLiveSessionBridge("conv-test", "tutor", "gemini-2.0-flash");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            bridge.ConnectAndSetupAsync("dummy-api-key", new { }, ct: cts.Token));
    }

    [Fact]
    public async Task ConnectThenDispose_EmitsMatchedPlusOneMinusOneExactlyOnce()
    {
        var measurements = new List<(string Name, object Value, KeyValuePair<string, object?>[] Tags)>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (i, l) =>
        {
            if (i.Meter.Name == AiTelemetry.MeterName)
                l.EnableMeasurementEvents(i);
        };
        meterListener.SetMeasurementEventCallback<long>((i, v, t, s) =>
        {
            lock (measurements) measurements.Add((i.Name, v, t.ToArray()));
        });
        meterListener.SetMeasurementEventCallback<double>((i, v, t, s) =>
        {
            lock (measurements) measurements.Add((i.Name, v, t.ToArray()));
        });
        meterListener.Start();

        var bridge = new TestableBridge("conv-1", "tutor", "gemini-bridge-test-1");
        bridge.ConnectCanCompleteTcs.TrySetResult(true);

        await bridge.ConnectAndSetupAsync("key", new { });
        await bridge.DisposeAsync();

        var activeCounters = measurements
            .Where(m => m.Name == "ept.livetutor.sessions.active" &&
                        m.Tags.Any(t => t.Key == "ept.livetutor.role" && (string?)t.Value == "tutor"))
            .Select(m => (long)m.Value)
            .ToList();

        Assert.Equal(2, activeCounters.Count);
        Assert.Equal(1L, activeCounters[0]);
        Assert.Equal(-1L, activeCounters[1]);

        var durationMeasurements = measurements
            .Where(m => m.Name == "ept.livetutor.session.duration" &&
                        m.Tags.Any(t => t.Key == "ept.livetutor.role" && (string?)t.Value == "tutor"))
            .ToList();
        Assert.NotEmpty(durationMeasurements);
    }

    [Fact]
    public async Task DisposeTwice_MinusOneEmittedOnlyOnce()
    {
        var measurements = new List<(string Name, object Value, KeyValuePair<string, object?>[] Tags)>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (i, l) =>
        {
            if (i.Meter.Name == AiTelemetry.MeterName)
                l.EnableMeasurementEvents(i);
        };
        meterListener.SetMeasurementEventCallback<long>((i, v, t, s) =>
        {
            lock (measurements) measurements.Add((i.Name, v, t.ToArray()));
        });
        meterListener.SetMeasurementEventCallback<double>((i, v, t, s) =>
        {
            lock (measurements) measurements.Add((i.Name, v, t.ToArray()));
        });
        meterListener.Start();

        var bridge = new TestableBridge("conv-2", "transcribe", "gemini-bridge-test-2");
        bridge.ConnectCanCompleteTcs.TrySetResult(true);

        await bridge.ConnectAndSetupAsync("key", new { });
        await bridge.DisposeAsync();
        await bridge.DisposeAsync();

        var minusOnes = measurements
            .Where(m => m.Name == "ept.livetutor.sessions.active" &&
                        m.Tags.Any(t => t.Key == "ept.livetutor.role" && (string?)t.Value == "transcribe") &&
                        (long)m.Value == -1L)
            .ToList();

        Assert.Single(minusOnes);
    }

    [Fact]
    public async Task DisposeDuringConnect_CounterRemainsBalanced()
    {
        var measurements = new List<(string Name, object Value, KeyValuePair<string, object?>[] Tags)>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (i, l) =>
        {
            if (i.Meter.Name == AiTelemetry.MeterName)
                l.EnableMeasurementEvents(i);
        };
        meterListener.SetMeasurementEventCallback<long>((i, v, t, s) =>
        {
            lock (measurements) measurements.Add((i.Name, v, t.ToArray()));
        });
        meterListener.SetMeasurementEventCallback<double>((i, v, t, s) =>
        {
            lock (measurements) measurements.Add((i.Name, v, t.ToArray()));
        });
        meterListener.Start();

        var bridge = new TestableBridge("conv-3", "translate", "gemini-bridge-test-3");

        var connectTask = bridge.ConnectAndSetupAsync("key", new { });
        await bridge.ConnectStartedTcs.Task;

        var disposeTask = bridge.DisposeAsync().AsTask();
        bridge.ConnectCanCompleteTcs.TrySetResult(true);

        await Task.WhenAll(connectTask, disposeTask);

        var activeCounters = measurements
            .Where(m => m.Name == "ept.livetutor.sessions.active" &&
                        m.Tags.Any(t => t.Key == "ept.livetutor.role" && (string?)t.Value == "translate"))
            .Select(m => (long)m.Value)
            .ToList();

        Assert.Equal(0L, activeCounters.Sum());
        if (activeCounters.Count > 0)
        {
            Assert.Equal(2, activeCounters.Count);
            Assert.Equal(1L, activeCounters[0]);
            Assert.Equal(-1L, activeCounters[1]);
        }
    }

    private sealed class TestableBridge : GeminiLiveSessionBridge
    {
        public TaskCompletionSource<bool> ConnectStartedTcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> ConnectCanCompleteTcs { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TestableBridge(string conversationId, string roleTag, string? modelCode = null)
            : base(conversationId, roleTag, modelCode) { }

        protected override async Task ConnectInternalAsync(CancellationToken ct)
        {
            ConnectStartedTcs.TrySetResult(true);
            using var reg = ct.Register(() => ConnectCanCompleteTcs.TrySetCanceled(ct));
            await ConnectCanCompleteTcs.Task;
        }
    }
}
