using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Curriculum.Domain.Vocabularies;

/// <summary>
/// A vocabulary word/phrase with definitions, examples, and pronunciation.
/// Core content unit for language learning.
/// Synonyms are stored as a flat string array (PostgreSQL array column) — no join table needed.
/// </summary>
public sealed class Vocabulary : AggregateRoot
{
    public string Word { get; private set; } = null!;
    public string Definition { get; private set; } = null!;
    public string LanguageCode { get; private set; } = "en";
    public string? Pronunciation { get; private set; }
    public string? PhoneticTranscription { get; private set; }
    public string? PartOfSpeech { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? AudioUrl { get; private set; }
    public string? Level { get; private set; }
    public Guid? TopicId { get; private set; }

    /// <summary>
    /// Synonyms stored as a PostgreSQL text array — e.g. {"happy", "joyful", "glad"}.
    /// No separate join table required.
    /// </summary>
    public List<string> Synonyms { get; private set; } = [];
    /// <summary>True when this entry was created by AI generation, not manually entered.</summary>
    public bool IsAiGenerated { get; private set; }

    private Vocabulary() { }

    public static Vocabulary Create(
        string word,
        string definition,
        string languageCode,
        string? pronunciation = null,
        string? phoneticTranscription = null,
        string? partOfSpeech = null,
        string? level = null,
        Guid? topicId = null,
        List<string>? synonyms = null,
        bool isAiGenerated = false)
    {
        var entity = new Vocabulary
        {
            Id = Guid.NewGuid(),
            Word = word.Trim(),
            Definition = definition.Trim(),
            LanguageCode = languageCode.ToLowerInvariant(),
            Pronunciation = pronunciation,
            PhoneticTranscription = phoneticTranscription,
            PartOfSpeech = partOfSpeech,
            Level = level,
            TopicId = topicId,
            Synonyms = synonyms ?? [],
            IsAiGenerated = isAiGenerated
        };

        entity.RaiseDomainEvent(new Events.VocabularyCreatedDomainEvent(entity.Id));
        return entity;
    }

    public void Update(
        string word,
        string definition,
        string? pronunciation,
        string? phoneticTranscription,
        string? partOfSpeech,
        string? imageUrl,
        string? audioUrl,
        string? level,
        Guid? topicId,
        List<string>? synonyms = null)
    {
        Word = word.Trim();
        Definition = definition.Trim();
        Pronunciation = pronunciation;
        PhoneticTranscription = phoneticTranscription;
        PartOfSpeech = partOfSpeech;
        ImageUrl = imageUrl;
        AudioUrl = audioUrl;
        Level = level;
        TopicId = topicId;
        Synonyms = synonyms ?? Synonyms;
    }

    public void AddSynonym(string word)
    {
        if (!Synonyms.Contains(word.Trim()))
            Synonyms.Add(word.Trim());
    }

    public void RemoveSynonym(string word) => Synonyms.Remove(word.Trim());
}
