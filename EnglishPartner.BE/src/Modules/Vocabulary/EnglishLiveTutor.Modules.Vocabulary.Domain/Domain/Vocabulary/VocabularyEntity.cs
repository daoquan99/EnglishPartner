using EnglishLiveTutor.Domain.Common.Primitives;
using EnglishLiveTutor.Domain.Vocabulary.Entities;
using EnglishLiveTutor.Domain.Vocabulary.Enums;

namespace EnglishLiveTutor.Domain.Vocabulary;

public sealed class VocabularyEntity : AggregateRoot<Guid>
{
    private List<VocabularySenseEntity> _senses = new();
    private List<VocabularyPronunciationEntity> _pronunciations = new();
    private List<VocabularyWordFamilyEntity> _wordFamilies = new();

    public string Word { get; private set; } = string.Empty;
    public string NormalizedWord { get; private set; } = string.Empty;
    public string LanguageCode { get; private set; } = "en";
    public int? FrequencyRank { get; private set; }
    public string SourceType { get; private set; } = nameof(VocabularySourceType.Seeded); // Seeded | AI | Imported | Manual
    public string? SourceModelId { get; private set; }
    public string? TopicCategory { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<VocabularySenseEntity> Senses => _senses.AsReadOnly();
    public IReadOnlyCollection<VocabularyPronunciationEntity> Pronunciations => _pronunciations.AsReadOnly();
    public IReadOnlyCollection<VocabularyWordFamilyEntity> WordFamilies => _wordFamilies.AsReadOnly();

    private VocabularyEntity()
    {
    }

    public VocabularyEntity(
        string word,
        string languageCode = "en",
        string sourceType = "Seeded",
        int? frequencyRank = null,
        string? sourceModelId = null,
        string? topicCategory = null,
        bool isActive = true,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        string normalized = NormalizeWord(word);
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException("Từ vựng không được để trống.");

        Word = word.Trim();
        NormalizedWord = normalized;
        LanguageCode = string.IsNullOrWhiteSpace(languageCode) ? "en" : languageCode.Trim().ToLowerInvariant();
        SourceType = string.IsNullOrWhiteSpace(sourceType) ? nameof(VocabularySourceType.Seeded) : sourceType.Trim();
        FrequencyRank = frequencyRank;
        SourceModelId = string.IsNullOrWhiteSpace(sourceModelId) ? null : sourceModelId.Trim();
        TopicCategory = string.IsNullOrWhiteSpace(topicCategory) ? null : topicCategory.Trim();
        IsActive = isActive;
    }

    public static VocabularyEntity Create(
        string word,
        string languageCode = "en",
        string sourceType = "Seeded",
        int? frequencyRank = null,
        string? sourceModelId = null,
        string? topicCategory = null,
        bool isActive = true,
        Guid? id = null) =>
        new(word, languageCode, sourceType, frequencyRank, sourceModelId, topicCategory, isActive, id);

    public VocabularySenseEntity AddSense(
        string partOfSpeech,
        string definitionEn,
        string meaningVi,
        string cefrLevel = "B1",
        string? descriptionEn = null,
        bool? isPrimary = null,
        int? displayOrder = null,
        Guid? id = null)
    {
        bool primary = isPrimary ?? (_senses.Count == 0);
        if (primary && _senses.Any(s => s.IsPrimary))
        {
            foreach (var existing in _senses)
            {
                existing.SetPrimary(false);
            }
        }

        int order = displayOrder ?? (_senses.Count == 0 ? 1 : _senses.Max(s => s.DisplayOrder) + 1);
        var sense = new VocabularySenseEntity(
            vocabularyId: Id,
            partOfSpeech: partOfSpeech,
            definitionEn: definitionEn,
            meaningVi: meaningVi,
            cefrLevel: cefrLevel,
            descriptionEn: descriptionEn,
            isPrimary: primary,
            displayOrder: order,
            id: id);

        _senses.Add(sense);
        return sense;
    }

    public VocabularyPronunciationEntity AddPronunciation(
        string accent,
        string ipa,
        string audioSource = "BrowserTTS",
        string? audioUrl = null,
        int? syllableCount = null,
        string? stressPattern = null,
        Guid? id = null)
    {
        var pronunciation = new VocabularyPronunciationEntity(
            vocabularyId: Id,
            accent: accent,
            ipa: ipa,
            audioSource: audioSource,
            audioUrl: audioUrl,
            syllableCount: syllableCount,
            stressPattern: stressPattern,
            id: id);

        _pronunciations.Add(pronunciation);
        return pronunciation;
    }

    public VocabularyWordFamilyEntity AddWordFamily(
        string relatedWord,
        string partOfSpeech = "noun",
        Guid? relatedVocabularyId = null,
        Guid? id = null)
    {
        var wf = new VocabularyWordFamilyEntity(
            vocabularyId: Id,
            relatedWord: relatedWord,
            partOfSpeech: partOfSpeech,
            relatedVocabularyId: relatedVocabularyId,
            id: id);

        _wordFamilies.Add(wf);
        return wf;
    }

    public void UpdateDefinition(
        Guid senseId,
        string definitionEn,
        string meaningVi,
        string? descriptionEn = null)
    {
        var sense = _senses.FirstOrDefault(s => s.Id == senseId)
            ?? throw new InvalidOperationException($"Không tìm thấy nghĩa từ vựng (SenseId: {senseId}).");

        sense.UpdateDefinition(definitionEn, meaningVi, descriptionEn);
    }

    public void Activate() => IsActive = true;

    public void UpdateDetails(string word, int? frequencyRank, string? topicCategory)
    {
        string normalized = NormalizeWord(word);
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException("Từ vựng không được để trống.");

        Word = word.Trim();
        NormalizedWord = normalized;
        FrequencyRank = frequencyRank;
        TopicCategory = string.IsNullOrWhiteSpace(topicCategory) ? null : topicCategory.Trim();
        SourceType = "AdminUpdated";
    }

    public void ClearPronunciationsAndWordFamilies()
    {
        _pronunciations.Clear();
        _wordFamilies.Clear();
    }

    public void Deactivate() => IsActive = false;

    public static string NormalizeWord(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        return string.Join(' ', raw.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
