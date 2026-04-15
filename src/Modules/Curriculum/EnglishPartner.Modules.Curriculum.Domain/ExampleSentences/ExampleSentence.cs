using EnglishPartner.SharedKernel.Domain;
using EnglishPartner.Modules.Curriculum.Domain.ExampleSentences.Entities;

namespace EnglishPartner.Modules.Curriculum.Domain.ExampleSentences;

/// <summary>
/// A standalone example sentence that can be linked to topics, vocabularies, or grammars.
/// Used for rich context building.
/// </summary>
public sealed class ExampleSentence : AggregateRoot
{
    public string Text { get; private set; } = null!;
    public string LanguageCode { get; private set; } = "en";
    public string? AudioUrl { get; private set; }
    public string? Note { get; private set; }
    public string? Level { get; private set; }

    // Relationships to organize where this example can appear
    public Guid? TopicId { get; private set; }
    public Guid? VocabularyId { get; private set; }
    public Guid? GrammarId { get; private set; }
    /// <summary>True when this entry was created by AI generation, not manually entered.</summary>
    public bool IsAiGenerated { get; private set; }

    private readonly List<ExampleSentenceTranslation> _translations = [];
    public IReadOnlyList<ExampleSentenceTranslation> Translations => _translations.AsReadOnly();

    private ExampleSentence() { }

    public static ExampleSentence Create(
        string text,
        string languageCode,
        string? audioUrl = null,
        string? note = null,
        string? level = null,
        Guid? topicId = null,
        Guid? vocabularyId = null,
        Guid? grammarId = null,
        bool isAiGenerated = false)
    {
        return new ExampleSentence
        {
            Id = Guid.NewGuid(),
            Text = text.Trim(),
            LanguageCode = languageCode.ToLowerInvariant(),
            AudioUrl = audioUrl,
            Note = note?.Trim(),
            Level = level,
            TopicId = topicId,
            VocabularyId = vocabularyId,
            GrammarId = grammarId,
            IsAiGenerated = isAiGenerated
        };
    }

    public void Update(
        string text,
        string? audioUrl,
        string? note,
        string? level,
        Guid? topicId,
        Guid? vocabularyId,
        Guid? grammarId)
    {
        Text = text.Trim();
        AudioUrl = audioUrl;
        Note = note?.Trim();
        Level = level;
        TopicId = topicId;
        VocabularyId = vocabularyId;
        GrammarId = grammarId;
    }

    public void AddTranslation(string languageCode, string translatedText)
    {
        var existing = _translations.FirstOrDefault(t => t.LanguageCode == languageCode);
        if (existing is not null) _translations.Remove(existing);
        _translations.Add(ExampleSentenceTranslation.Create(Id, languageCode, translatedText));
    }
}
