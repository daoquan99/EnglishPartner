using System.Diagnostics;
using OpenTelemetry;

namespace EnglishLiveTutor.Infrastructure.Observability;

public sealed class HubStreamFilteringProcessor : BaseProcessor<Activity>
{
    private static readonly HashSet<string> DroppedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "StreamUserAudioToTutor",
        "StreamAudioForTranscription",
        "StreamAudioForTranslation"
    };

    public override void OnEnd(Activity activity)
    {
        if (activity.GetTagItem("rpc.method") is string method && DroppedMethods.Contains(method))
        {
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }
    }
}
