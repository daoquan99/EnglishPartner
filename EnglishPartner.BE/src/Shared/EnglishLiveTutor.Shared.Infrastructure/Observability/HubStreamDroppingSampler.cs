using OpenTelemetry.Trace;

namespace EnglishLiveTutor.Infrastructure.Observability;

public sealed class HubStreamDroppingSampler : Sampler
{
    private static readonly HashSet<string> DroppedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "StreamUserAudioToTutor",
        "StreamAudioForTranscription",
        "StreamAudioForTranslation"
    };

    private static readonly string[] DroppedStreamSuffixes =
    {
        "/StreamUserAudioToTutor",
        "/StreamAudioForTranscription",
        "/StreamAudioForTranslation"
    };

    private readonly Sampler _innerSampler;

    public HubStreamDroppingSampler(Sampler innerSampler)
    {
        _innerSampler = innerSampler ?? throw new ArgumentNullException(nameof(innerSampler));
    }

    public Sampler InnerSampler => _innerSampler;

    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        string name = samplingParameters.Name ?? string.Empty;

        // .NET 10 SignalR activity name check
        if (string.Equals(name, "Microsoft.AspNetCore.SignalR.Server.InvocationIn", StringComparison.Ordinal)
            && samplingParameters.Tags != null)
        {
            foreach (var tag in samplingParameters.Tags)
            {
                if (string.Equals(tag.Key, "rpc.method", StringComparison.OrdinalIgnoreCase)
                    && tag.Value is string method
                    && DroppedMethods.Contains(method))
                {
                    return new SamplingResult(SamplingDecision.Drop);
                }
            }
        }

        // Fallback: EndsWith check on Activity Name or DisplayName
        for (int i = 0; i < DroppedStreamSuffixes.Length; i++)
        {
            if (name.EndsWith(DroppedStreamSuffixes[i], StringComparison.OrdinalIgnoreCase))
            {
                return new SamplingResult(SamplingDecision.Drop);
            }
        }

        return _innerSampler.ShouldSample(samplingParameters);
    }
}
