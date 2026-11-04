using System.Diagnostics;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Tests.Infrastructure;
using EnglishLiveTutor.Infrastructure.Observability;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Xunit;

namespace EnglishLiveTutor.Api.IntegrationTests.Observability;

/// <summary>
/// Drives real hub invocations through a real OpenTelemetry pipeline (app sampler + filtering
/// processor + exporter), so the assertion is on what actually reaches an exporter.
/// </summary>
[Collection("LiveTelemetry")]
public sealed class SignalRHubStreamExportPipelineTests
{
    private const string SignalRSource = "Microsoft.AspNetCore.SignalR.Server";

    [Fact]
    public async Task StreamAudioInvocations_AreNotExported_WhileOtherHubInvocationsAre()
    {
        var exported = new List<Activity>();
        var spy = new SamplingSpy(new HubStreamDroppingSampler(new AlwaysOnSampler()));

        using (var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(SignalRSource)
            .SetSampler(spy)
            .AddProcessor(new HubStreamFilteringProcessor())
            .AddProcessor(new SimpleActivityExportProcessor(new CollectingExporter(exported)))
            .Build())
        {
            using var factory = new ApiTestFactory();
            var server = factory.Server;
            var token = factory.CreateAccessToken(userId: Guid.NewGuid());

            var connection = new HubConnectionBuilder()
                .WithUrl(new Uri(server.BaseAddress, "/hubs/tutor"), options =>
                {
                    options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                    options.Transports = HttpTransportType.LongPolling;
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                })
                .Build();

            await connection.StartAsync();
            var payload = new AudioChunkPayload(
                ConversationId: "conv-export-pipeline",
                Speaker: "user",
                TurnId: "turn-01",
                Base64Pcm16k: Convert.ToBase64String(new byte[32]));

            for (int i = 0; i < 3; i++)
            {
                try { await connection.InvokeAsync("StreamUserAudioToTutor", payload); } catch { /* business error is fine */ }
            }

            try { await connection.InvokeAsync("NotifyUserSpeechEnd", "conv-export-pipeline"); } catch { /* business error is fine */ }

            await connection.StopAsync();
            await connection.DisposeAsync();
            tracerProvider.ForceFlush();
        }

        List<string?> exportedMethods;
        lock (exported)
        {
            exportedMethods = exported
                .Where(a => a.OperationName == "Microsoft.AspNetCore.SignalR.Server.InvocationIn")
                .Select(a => a.GetTagItem("rpc.method") as string)
                .ToList();
        }

        Assert.Contains("NotifyUserSpeechEnd", exportedMethods);
        Assert.DoesNotContain("StreamUserAudioToTutor", exportedMethods);

        // Records whether rpc.method is already visible at sampling time (the sampler path) or only
        // at end (the processor path). Either way nothing is exported, asserted above.
        Assert.Contains(spy.InvocationSamplings, s => s.Method == "StreamUserAudioToTutor" && s.Decision == SamplingDecision.Drop);
    }

    private sealed class SamplingSpy(Sampler inner) : Sampler
    {
        public List<(string? Method, SamplingDecision Decision)> InvocationSamplings { get; } = new();

        public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
        {
            var result = inner.ShouldSample(samplingParameters);
            if (samplingParameters.Name == "Microsoft.AspNetCore.SignalR.Server.InvocationIn")
            {
                string? method = samplingParameters.Tags?
                    .FirstOrDefault(t => t.Key == "rpc.method").Value as string;
                lock (InvocationSamplings)
                {
                    InvocationSamplings.Add((method, result.Decision));
                }
            }

            return result;
        }
    }

    private sealed class CollectingExporter(List<Activity> sink) : BaseExporter<Activity>
    {
        public override ExportResult Export(in Batch<Activity> batch)
        {
            lock (sink)
            {
                foreach (var activity in batch)
                {
                    sink.Add(activity);
                }
            }

            return ExportResult.Success;
        }
    }
}
