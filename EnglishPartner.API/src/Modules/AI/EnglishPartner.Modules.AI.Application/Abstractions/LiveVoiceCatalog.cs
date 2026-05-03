namespace EnglishPartner.Modules.AI.Application.Abstractions;

public record LiveVoice(string Id, string DisplayName, string Gender);

/// <summary>
/// Catalog of available Native Audio voices for Gemini Live.
/// Used to validate input voices and provide details for Gemini TTS.
/// </summary>
public static class LiveVoiceCatalog
{
    /// <summary>
    /// The default voice used if an invalid or null voice is provided.
    /// </summary>
    public const string DefaultVoice = "zephyr";

    /// <summary>
    /// List of all supported native audio voices on Google AI Studio / Vertex AI.
    /// </summary>
    public static IReadOnlyList<LiveVoice> All { get; } =
    [
        new("achernar", "Achernar", "Female"),
        new("achird", "Achird", "Male"),
        new("algenib", "Algenib", "Male"),
        new("algieba", "Algieba", "Male"),
        new("alnilam", "Alnilam", "Male"),
        new("aoede", "Aoede", "Female"),
        new("autonoe", "Autonoe", "Female"),
        new("callirrhoe", "Callirrhoe", "Female"),
        new("charon", "Charon", "Male"),
        new("despina", "Despina", "Female"),
        new("enceladus", "Enceladus", "Male"),
        new("erinome", "Erinome", "Female"),
        new("fenrir", "Fenrir", "Male"),
        new("gacrux", "Gacrux", "Female"),
        new("iapetus", "Iapetus", "Male"),
        new("kore", "Kore", "Female"),
        new("laomedeia", "Laomedeia", "Female"),
        new("leda", "Leda", "Female"),
        new("orus", "Orus", "Male"),
        new("puck", "Puck", "Male"),
        new("pulcherrima", "Pulcherrima", "Female"),
        new("rasalgethi", "Rasalgethi", "Male"),
        new("sadachbia", "Sadachbia", "Male"),
        new("sadaltager", "Sadaltager", "Male"),
        new("schedar", "Schedar", "Male"),
        new("sulafat", "Sulafat", "Female"),
        new("umbriel", "Umbriel", "Male"),
        new("vindemiatrix", "Vindemiatrix", "Female"),
        new("zephyr", "Zephyr", "Female"),
        new("zubenelgenubi", "Zubenelgenubi", "Male")
    ];

    /// <summary>
    /// Validates the provided voice name against the catalog.
    /// Returns the validated voice name (lowercase) or the DefaultVoice if invalid.
    /// </summary>
    public static string GetValidVoiceOrDefault(string? voiceName)
    {
        var match = FindById(voiceName);
        return match?.Id ?? DefaultVoice;
    }

    /// <summary>
    /// Finds a LiveVoice by its ID.
    /// </summary>
    public static LiveVoice? FindById(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        return All.FirstOrDefault(v => v.Id.Equals(id.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
