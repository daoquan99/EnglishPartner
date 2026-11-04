using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Vocabulary.Entities;

public sealed class VocabularyPronunciationEntity : Entity<Guid>
{
    public Guid VocabularyId { get; private set; }
    public string Accent { get; private set; } = "US"; // UK | US
    public string Ipa { get; private set; } = string.Empty;
    public string? AudioUrl { get; private set; }
    public string AudioSource { get; private set; } = "BrowserTTS";
    public int? SyllableCount { get; private set; }
    public string? StressPattern { get; private set; }

    private VocabularyPronunciationEntity()
    {
    }

    internal VocabularyPronunciationEntity(
        Guid vocabularyId,
        string accent,
        string ipa,
        string audioSource = "BrowserTTS",
        string? audioUrl = null,
        int? syllableCount = null,
        string? stressPattern = null,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(ipa))
            throw new InvalidOperationException("IPA không được để trống.");

        VocabularyId = vocabularyId;
        Accent = string.IsNullOrWhiteSpace(accent) ? "US" : accent.Trim().ToUpperInvariant();
        Ipa = ipa.Trim();
        AudioSource = string.IsNullOrWhiteSpace(audioSource) ? "BrowserTTS" : audioSource.Trim();
        AudioUrl = string.IsNullOrWhiteSpace(audioUrl) ? null : audioUrl.Trim();
        SyllableCount = syllableCount;
        StressPattern = string.IsNullOrWhiteSpace(stressPattern) ? null : stressPattern.Trim();
    }
}
