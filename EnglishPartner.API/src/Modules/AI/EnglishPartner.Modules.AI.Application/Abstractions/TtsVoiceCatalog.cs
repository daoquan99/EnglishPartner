namespace EnglishPartner.Modules.AI.Application.Abstractions;

/// <summary>
/// Represents a selectable TTS voice with a user-friendly name.
/// </summary>
public sealed record TtsVoice(
    string Id,              // Google voice name — passed to TTS API
    string DisplayName,     // e.g. "Aria (Female)"
    string Accent,          // e.g. "American", "British"
    string Gender,          // "Male" | "Female"
    string LanguageCode);   // e.g. "en-US"

/// <summary>
/// Catalog of available TTS voices, organized for FE selection.
/// Static — no DB needed. Update here when Google adds new voices.
/// </summary>
public static class TtsVoiceCatalog
{
    public static IReadOnlyList<TtsVoice> All { get; } =
    [
        // ── 🇺🇸 American English ──────────────────────────────────────────────
        new("en-US-Neural2-A", "Ava",    "American", "Female", "en-US"),
        new("en-US-Neural2-C", "Clara",  "American", "Female", "en-US"),
        new("en-US-Neural2-D", "David",  "American", "Male",   "en-US"),
        new("en-US-Neural2-E", "Emma",   "American", "Female", "en-US"),
        new("en-US-Neural2-F", "Fiona",  "American", "Female", "en-US"),
        new("en-US-Neural2-G", "Grace",  "American", "Female", "en-US"),
        new("en-US-Neural2-H", "Hazel",  "American", "Female", "en-US"),
        new("en-US-Neural2-I", "Ian",    "American", "Male",   "en-US"),
        new("en-US-Neural2-J", "James",  "American", "Male",   "en-US"),

        // ── 🇬🇧 British English ───────────────────────────────────────────────
        new("en-GB-Neural2-A", "Alice",  "British",   "Female", "en-GB"),
        new("en-GB-Neural2-B", "Ben",    "British",   "Male",   "en-GB"),
        new("en-GB-Neural2-C", "Chloe", "British",   "Female", "en-GB"),
        new("en-GB-Neural2-D", "Daniel","British",   "Male",   "en-GB"),
        new("en-GB-Neural2-F", "Faith", "British",   "Female", "en-GB"),

        // ── 🇦🇺 Australian English ────────────────────────────────────────────
        new("en-AU-Neural2-A", "Amelia", "Australian", "Female", "en-AU"),
        new("en-AU-Neural2-B", "Blake",  "Australian", "Male",   "en-AU"),
        new("en-AU-Neural2-C", "Cara",   "Australian", "Female", "en-AU"),
        new("en-AU-Neural2-D", "Dylan",  "Australian", "Male",   "en-AU"),
    ];

    /// <summary>Find a voice by its Google ID. Returns null if not found.</summary>
    public static TtsVoice? FindById(string id) =>
        All.FirstOrDefault(v => v.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
}
