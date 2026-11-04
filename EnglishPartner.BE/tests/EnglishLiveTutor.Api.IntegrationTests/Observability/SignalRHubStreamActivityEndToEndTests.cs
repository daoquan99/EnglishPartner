using System.Diagnostics;
using EnglishLiveTutor.Domain.Tests.Infrastructure;
using EnglishLiveTutor.Infrastructure.Observability;
using EnglishLiveTutor.Application.Common.Models;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using OpenTelemetry.Trace;
using Xunit;

namespace EnglishLiveTutor.Api.IntegrationTests.Observability;

[Collection("LiveTelemetry")]
public sealed class SignalRHubStreamActivityEndToEndTests
{
    [Fact]
    public async Task SignalR_StreamUserAudioToTutor_ActivityIsObservedAndDroppedByAppSamplerAndProcessor()
    {
        using var factory = new ApiTestFactory();
        var server = factory.Server;
        var userId = Guid.NewGuid();
        var token = factory.CreateAccessToken(userId: userId);

        var capturedActivities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == "Microsoft.AspNetCore.SignalR.Server",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a =>
            {
                lock (capturedActivities)
                {
                    capturedActivities.Add(a);
                }
            }
        };
        ActivitySource.AddActivityListener(listener);

        var hubUrl = new Uri(server.BaseAddress, "/hubs/tutor");
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .Build();

        await connection.StartAsync();

        // 1. Invoke streaming method (StreamUserAudioToTutor)
        var streamPayload = new AudioChunkPayload(
            ConversationId: "conv-obs-test-01",
            Speaker: "user",
            TurnId: "turn-01",
            Base64Pcm16k: Convert.ToBase64String(new byte[32]));

        try
        {
            await connection.InvokeAsync("StreamUserAudioToTutor", streamPayload);
        }
        catch
        {
            // Business layer exception is acceptable; invocation activity is emitted regardless
        }

        // 2. Invoke non-streaming method (NotifyUserSpeechEnd)
        try
        {
            await connection.InvokeAsync("NotifyUserSpeechEnd", "conv-obs-test-01");
        }
        catch
        {
            // Business layer exception is acceptable
        }

        await connection.StopAsync();
        await connection.DisposeAsync();

        // Give any in-flight activities a moment to complete
        await Task.Delay(100);

        // 3. Find and verify the stream invocation activity
        Activity? streamActivity;
        lock (capturedActivities)
        {
            streamActivity = capturedActivities.FirstOrDefault(a =>
                string.Equals(a.GetTagItem("rpc.method") as string, "StreamUserAudioToTutor", StringComparison.OrdinalIgnoreCase));
        }

        Assert.NotNull(streamActivity);
        // Assert the REAL activity name from .NET 10 SignalR server
        Assert.Equal("Microsoft.AspNetCore.SignalR.Server.InvocationIn", streamActivity.OperationName);
        Assert.Equal("StreamUserAudioToTutor", streamActivity.GetTagItem("rpc.method"));

        // 4. Find and verify the non-stream invocation activity
        Activity? nonStreamActivity;
        lock (capturedActivities)
        {
            nonStreamActivity = capturedActivities.FirstOrDefault(a =>
                string.Equals(a.GetTagItem("rpc.method") as string, "NotifyUserSpeechEnd", StringComparison.OrdinalIgnoreCase));
        }

        Assert.NotNull(nonStreamActivity);
        Assert.Equal("Microsoft.AspNetCore.SignalR.Server.InvocationIn", nonStreamActivity.OperationName);
        Assert.Equal("NotifyUserSpeechEnd", nonStreamActivity.GetTagItem("rpc.method"));

        // 5. Pass exact parameters from captured stream activity through app's sampler
        var innerSampler = new AlwaysOnSampler();
        var appSampler = new HubStreamDroppingSampler(innerSampler);

        var streamSamplingParams = new SamplingParameters(
            default,
            streamActivity.TraceId,
            streamActivity.OperationName,
            streamActivity.Kind,
            streamActivity.TagObjects);

        var streamSampleResult = appSampler.ShouldSample(streamSamplingParams);
        Assert.Equal(SamplingDecision.Drop, streamSampleResult.Decision);

        var nonStreamSamplingParams = new SamplingParameters(
            default,
            nonStreamActivity.TraceId,
            nonStreamActivity.OperationName,
            nonStreamActivity.Kind,
            nonStreamActivity.TagObjects);

        var nonStreamSampleResult = appSampler.ShouldSample(nonStreamSamplingParams);
        Assert.Equal(SamplingDecision.RecordAndSample, nonStreamSampleResult.Decision);

        // 6. Pass exact activities through app's processor
        var processor = new HubStreamFilteringProcessor();

        // Verify processor removes Recorded flag from stream activity
        streamActivity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
        processor.OnEnd(streamActivity);
        Assert.True((streamActivity.ActivityTraceFlags & ActivityTraceFlags.Recorded) == 0,
            "Stream activity Recorded flag should be cleared by HubStreamFilteringProcessor");

        // Verify processor preserves Recorded flag on non-stream activity
        nonStreamActivity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
        processor.OnEnd(nonStreamActivity);
        Assert.True((nonStreamActivity.ActivityTraceFlags & ActivityTraceFlags.Recorded) != 0,
            "Non-stream activity Recorded flag should be retained by HubStreamFilteringProcessor");
    }
}
