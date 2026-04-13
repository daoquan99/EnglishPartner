using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Curriculum.Domain.ExampleSentences;

/// <summary>
/// A standalone example sentence that can be linked to topics, vocabularies, or grammars.
/// Used for rich context building.
/// </summary>
public sealed class ExampleSentence : AggregateRoot
{
    public string Text { get; private set; } = null!;
    public string? Translation { get; private set; }
    public string LanguageCode { get; private set; } = "en";
    public string? AudioUrl { get; private set; }
    public string? Note { get; private set; }
    public string? Level { get; private set; }
    
    // Relationships to organize where this example can appear
    public Guid? TopicId { get; private set; }
    public Guid? VocabularyId { get; private set; }
    public Guid? GrammarId { get; private set; }

    private ExampleSentence() { }

    public static ExampleSentence Create(
        string text,
        string languageCode,
        string? translation = null,
        string? audioUrl = null,
        string? note = null,
        string? level = null,
        Guid? topicId = null,
        Guid? vocabularyId = null,
        Guid? grammarId = null)
    {
        return new ExampleSentence
        {
            Id = Guid.NewGuid(),
            Text = text.Trim(),
            LanguageCode = languageCode.ToLowerInvariant(),
            Translation = translation?.Trim(),
            AudioUrl = audioUrl,
            Note = note?.Trim(),
            Level = level,
            TopicId = topicId,
            VocabularyId = vocabularyId,
            GrammarId = grammarId
        };
    }

    public void Update(
        string text,
        string? translation,
        string? audioUrl,
        string? note,
        string? level,
        Guid? topicId,
        Guid? vocabularyId,
        Guid? grammarId)
    {
        Text = text.Trim();
        Translation = translation?.Trim();
        AudioUrl = audioUrl;
        Note = note?.Trim();
        Level = level;
        TopicId = topicId;
        VocabularyId = vocabularyId;
        GrammarId = grammarId;
    }
}
