using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Curriculum.Domain.Vocabularies;

/// <summary>
/// A vocabulary word/phrase with definitions, examples, and pronunciation.
/// Core content unit for language learning.
/// </summary>
public sealed class Vocabulary : AggregateRoot
{
    public string Word { get; private set; } = null!;
    public string Definition { get; private set; } = null!;
    public string LanguageCode { get; private set; } = "en";
    public string? Pronunciation { get; private set; }
    public string? PhoneticTranscription { get; private set; }
    public string? PartOfSpeech { get; private set; }
    public string? ExampleSentence { get; private set; }
    public string? ExampleTranslation { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? AudioUrl { get; private set; }
    public string? Level { get; private set; }
    public Guid? TopicId { get; private set; }

    private readonly List<Entities.VocabularySynonym> _synonyms = [];
    public IReadOnlyList<Entities.VocabularySynonym> Synonyms => _synonyms.AsReadOnly();

    private Vocabulary() { }

    public static Vocabulary Create(
        string word,
        string definition,
        string languageCode,
        string? pronunciation = null,
        string? phoneticTranscription = null,
        string? partOfSpeech = null,
        string? exampleSentence = null,
        string? exampleTranslation = null,
        string? level = null,
        Guid? topicId = null)
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
            ExampleSentence = exampleSentence,
            ExampleTranslation = exampleTranslation,
            Level = level,
            TopicId = topicId
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
        string? exampleSentence,
        string? exampleTranslation,
        string? imageUrl,
        string? audioUrl,
        string? level,
        Guid? topicId)
    {
        Word = word.Trim();
        Definition = definition.Trim();
        Pronunciation = pronunciation;
        PhoneticTranscription = phoneticTranscription;
        PartOfSpeech = partOfSpeech;
        ExampleSentence = exampleSentence;
        ExampleTranslation = exampleTranslation;
        ImageUrl = imageUrl;
        AudioUrl = audioUrl;
        Level = level;
        TopicId = topicId;
    }

    public void AddSynonym(string word)
    {
        if (_synonyms.All(s => s.SynonymWord != word))
            _synonyms.Add(Entities.VocabularySynonym.Create(Id, word));
    }

    public void RemoveSynonym(Guid synonymId)
    {
        var synonym = _synonyms.FirstOrDefault(s => s.Id == synonymId);
        if (synonym is not null) _synonyms.Remove(synonym);
    }
}
