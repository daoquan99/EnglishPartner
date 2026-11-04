using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace EnglishLiveTutor.Infrastructure.Observability;

public static class ObservabilityServiceCollectionExtensions
{
    public static WebApplicationBuilder AddEnglishPartnerObservability(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;
        var environment = builder.Environment;

        string? otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        string? otlpTracesEndpoint = configuration["OTEL_EXPORTER_OTLP_TRACES_ENDPOINT"]
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT");
        string? otlpMetricsEndpoint = configuration["OTEL_EXPORTER_OTLP_METRICS_ENDPOINT"]
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_METRICS_ENDPOINT");
        string? otlpLogsEndpoint = configuration["OTEL_EXPORTER_OTLP_LOGS_ENDPOINT"]
            ?? Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_LOGS_ENDPOINT");

        bool hasTracesEndpoint = !string.IsNullOrWhiteSpace(otlpTracesEndpoint) || !string.IsNullOrWhiteSpace(otlpEndpoint);
        bool hasMetricsEndpoint = !string.IsNullOrWhiteSpace(otlpMetricsEndpoint) || !string.IsNullOrWhiteSpace(otlpEndpoint);
        bool hasLogsEndpoint = !string.IsNullOrWhiteSpace(otlpLogsEndpoint) || !string.IsNullOrWhiteSpace(otlpEndpoint);

        // M4: If no OTLP endpoint is configured for traces, metrics, or logs, exit early without calling AddOpenTelemetry()
        if (!hasTracesEndpoint && !hasMetricsEndpoint && !hasLogsEndpoint)
        {
            return builder;
        }

        double defaultRatio = environment.IsDevelopment() ? 1.0 : 0.2;
        double configuredRatio = configuration.GetValue<double?>("Observability:TraceSampleRatio") ?? defaultRatio;
        double sampleRatio = Math.Clamp(configuredRatio, 0.0, 1.0);

        string serviceVersion = typeof(ObservabilityServiceCollectionExtensions).Assembly.GetName().Version?.ToString() ?? "1.0.0";
        string? configuredServiceName = configuration["OTEL_SERVICE_NAME"] ?? Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME");
        string serviceName = string.IsNullOrWhiteSpace(configuredServiceName) ? "EnglishLiveTutor.Api" : configuredServiceName.Trim();

        var ratioSampler = new TraceIdRatioBasedSampler(sampleRatio);
        var parentSampler = new ParentBasedSampler(
            rootSampler: ratioSampler,
            remoteParentSampled: ratioSampler,
            remoteParentNotSampled: ratioSampler);
        var hubStreamSampler = new HubStreamDroppingSampler(parentSampler);

        if (hasTracesEndpoint || hasMetricsEndpoint)
        {
            var otelBuilder = builder.Services.AddOpenTelemetry()
                .ConfigureResource(resource => resource
                    .AddService(serviceName: serviceName, serviceVersion: serviceVersion));

            if (hasTracesEndpoint)
            {
                otelBuilder.WithTracing(tracing =>
                {
                    tracing
                        .SetSampler(hubStreamSampler)
                        .AddAspNetCoreInstrumentation(options =>
                        {
                            options.Filter = httpContext =>
                            {
                                var path = httpContext.Request.Path.Value;
                                return path == null || !path.StartsWith("/health", StringComparison.OrdinalIgnoreCase);
                            };
                            options.RecordException = false;
                        })
                        .AddHttpClientInstrumentation(options =>
                        {
                            options.RecordException = false;
                        })
                        .AddSource("Microsoft.AspNetCore.SignalR.Server")
                        .AddSource(AiTelemetry.SourceName)
                        .AddProcessor(new HubStreamFilteringProcessor())
                        .AddOtlpExporter();
                });
            }

            if (hasMetricsEndpoint)
            {
                otelBuilder.WithMetrics(metrics =>
                {
                    metrics
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddMeter(AiTelemetry.MeterName)
                        .AddOtlpExporter();
                });
            }
        }

        if (hasLogsEndpoint)
        {
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = false;
                logging.AddOtlpExporter();
            });
        }

        return builder;
    }
}
