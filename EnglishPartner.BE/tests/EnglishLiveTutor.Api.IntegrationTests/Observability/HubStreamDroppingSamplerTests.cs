using System.Diagnostics;
using EnglishLiveTutor.Infrastructure.Observability;
using OpenTelemetry.Trace;
using Xunit;

namespace EnglishLiveTutor.Api.IntegrationTests.Observability;

public sealed class HubStreamDroppingSamplerTests
{
    private sealed class TrackingSampler : Sampler
    {
        public int CallCount { get; private set; }
        public SamplingResult ReturnedResult { get; set; } = new(SamplingDecision.RecordAndSample);

        public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
        {
            CallCount++;
            return ReturnedResult;
        }
    }

    [Theory]
    [InlineData("StreamUserAudioToTutor")]
    [InlineData("StreamAudioForTranscription")]
    [InlineData("StreamAudioForTranslation")]
    public void SignalRInvocationIn_WithStreamRpcMethodTag_IsDroppedImmediately(string rpcMethod)
    {
        var inner = new TrackingSampler();
        var sampler = new HubStreamDroppingSampler(inner);

        var tags = new List<KeyValuePair<string, object?>>
        {
            new("rpc.system", "signalr"),
            new("rpc.service", "TutorConversationHub"),
            new("rpc.method", rpcMethod)
        };

        var parameters = new SamplingParameters(
            default,
            ActivityTraceId.CreateRandom(),
            "Microsoft.AspNetCore.SignalR.Server.InvocationIn",
            ActivityKind.Server,
            tags);

        var result = sampler.ShouldSample(parameters);

        Assert.Equal(SamplingDecision.Drop, result.Decision);
        Assert.Equal(0, inner.CallCount);
    }

    [Fact]
    public void SignalRInvocationIn_WithNonStreamRpcMethodTag_PassesThroughToInnerSampler()
    {
        var inner = new TrackingSampler();
        var sampler = new HubStreamDroppingSampler(inner);

        var tags = new List<KeyValuePair<string, object?>>
        {
            new("rpc.system", "signalr"),
            new("rpc.service", "TutorConversationHub"),
            new("rpc.method", "NotifyUserSpeechEnd")
        };

        var parameters = new SamplingParameters(
            default,
            ActivityTraceId.CreateRandom(),
            "Microsoft.AspNetCore.SignalR.Server.InvocationIn",
            ActivityKind.Server,
            tags);

        var result = sampler.ShouldSample(parameters);

        Assert.Equal(SamplingDecision.RecordAndSample, result.Decision);
        Assert.Equal(1, inner.CallCount);
    }

    [Theory]
    [InlineData("Microsoft.AspNetCore.SignalR.Server/StreamUserAudioToTutor")]
    [InlineData("/StreamUserAudioToTutor")]
    [InlineData("Microsoft.AspNetCore.SignalR.Server/StreamAudioForTranscription")]
    [InlineData("/StreamAudioForTranscription")]
    [InlineData("Microsoft.AspNetCore.SignalR.Server/StreamAudioForTranslation")]
    [InlineData("/StreamAudioForTranslation")]
    public void HubStreamActivities_AreDroppedImmediately_WithoutCallingInnerSampler(string activityName)
    {
        var inner = new TrackingSampler();
        var sampler = new HubStreamDroppingSampler(inner);

        var parameters = new SamplingParameters(
            default,
            ActivityTraceId.CreateRandom(),
            activityName,
            ActivityKind.Server);

        var result = sampler.ShouldSample(parameters);

        Assert.Equal(SamplingDecision.Drop, result.Decision);
        Assert.Equal(0, inner.CallCount);
    }

    [Theory]
    [InlineData("Microsoft.AspNetCore.SignalR.Server/SendMessage")]
    [InlineData("/LiveTutorHub/Connect")]
    [InlineData("Microsoft.AspNetCore.Hosting.HttpRequestIn")]
    [InlineData("GET /api/vocabulary")]
    [InlineData("EnglishLiveTutor.Ai/generate_content")]
    public void NonStreamActivities_PassThroughToInnerSampler(string activityName)
    {
        var inner = new TrackingSampler();
        var sampler = new HubStreamDroppingSampler(inner);

        var parameters = new SamplingParameters(
            default,
            ActivityTraceId.CreateRandom(),
            activityName,
            ActivityKind.Server);

        var result = sampler.ShouldSample(parameters);

        Assert.Equal(SamplingDecision.RecordAndSample, result.Decision);
        Assert.Equal(1, inner.CallCount);
    }

    [Fact]
    public void ParentBasedConfiguration_UsesRatioForBothSampledAndNotSampledParent()
    {
        double ratio = 0.5;
        var ratioSampler = new TraceIdRatioBasedSampler(ratio);
        var parentSampler = new ParentBasedSampler(
            rootSampler: ratioSampler,
            remoteParentSampled: ratioSampler,
            remoteParentNotSampled: ratioSampler);

        var sampler = new HubStreamDroppingSampler(parentSampler);
        Assert.Same(parentSampler, sampler.InnerSampler);
    }

    [Fact]
    public void HubStreamFilteringProcessor_RemovesRecordedFlag_ForStreamMethods()
    {
        using var source = new ActivitySource("TestSource.StreamFiltering");
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "TestSource.StreamFiltering",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        var processor = new HubStreamFilteringProcessor();

        using (var act = source.StartActivity("Microsoft.AspNetCore.SignalR.Server.InvocationIn", ActivityKind.Server))
        {
            Assert.NotNull(act);
            act.SetTag("rpc.method", "StreamUserAudioToTutor");
            Assert.True((act.ActivityTraceFlags & ActivityTraceFlags.Recorded) != 0);

            processor.OnEnd(act);

            Assert.True((act.ActivityTraceFlags & ActivityTraceFlags.Recorded) == 0);
        }

        using (var nonStreamAct = source.StartActivity("Microsoft.AspNetCore.SignalR.Server.InvocationIn", ActivityKind.Server))
        {
            Assert.NotNull(nonStreamAct);
            nonStreamAct.SetTag("rpc.method", "NotifyUserSpeechEnd");
            Assert.True((nonStreamAct.ActivityTraceFlags & ActivityTraceFlags.Recorded) != 0);

            processor.OnEnd(nonStreamAct);

            Assert.True((nonStreamAct.ActivityTraceFlags & ActivityTraceFlags.Recorded) != 0);
        }
    }
}
