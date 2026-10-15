using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Vocabulary.Vocabularies.Entities;

public sealed class VocabularySenseEntity : Entity<Guid>
{
    private List<VocabularyRelationEntity> _relations = new();
    private List<VocabularyCollocationEntity> _collocations = new();
    private List<VocabularyExampleEntity> _examples = new();
    private List<VocabularyGrammarPatternEntity> _grammarPatterns = new();

    public Guid VocabularyId { get; private set; }
    public string PartOfSpeech { get; private set; } = "noun";
    public string DefinitionEn { get; private set; } = string.Empty;
    public string MeaningVi { get; private set; } = string.Empty;
    public string? DescriptionEn { get; private set; }
    public string CefrLevel { get; private set; } = "B1"; // A1, A2, B1, B2, C1, C2
    public bool IsPrimary { get; private set; } = true;
    public int DisplayOrder { get; private set; } = 1;

    public IReadOnlyCollection<VocabularyRelationEntity> Relations => _relations.AsReadOnly();
    public IReadOnlyCollection<VocabularyCollocationEntity> Collocations => _collocations.AsReadOnly();
    public IReadOnlyCollection<VocabularyExampleEntity> Examples => _examples.AsReadOnly();
    public IReadOnlyCollection<VocabularyGrammarPatternEntity> GrammarPatterns => _grammarPatterns.AsReadOnly();

    private VocabularySenseEntity()
    {
    }

    internal VocabularySenseEntity(
        Guid vocabularyId,
        string partOfSpeech,
        string definitionEn,
        string meaningVi,
        string cefrLevel = "B1",
        string? descriptionEn = null,
        bool isPrimary = true,
        int displayOrder = 1,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(definitionEn))
            throw new InvalidOperationException("DefinitionEn không được để trống.");
        if (string.IsNullOrWhiteSpace(meaningVi))
            throw new InvalidOperationException("MeaningVi không được để trống.");

        VocabularyId = vocabularyId;
        PartOfSpeech = string.IsNullOrWhiteSpace(partOfSpeech) ? "noun" : partOfSpeech.Trim().ToLowerInvariant();
        DefinitionEn = definitionEn.Trim();
        MeaningVi = meaningVi.Trim();
        DescriptionEn = string.IsNullOrWhiteSpace(descriptionEn) ? null : descriptionEn.Trim();
        CefrLevel = string.IsNullOrWhiteSpace(cefrLevel) ? "B1" : cefrLevel.Trim().ToUpperInvariant();
        IsPrimary = isPrimary;
        DisplayOrder = displayOrder <= 0 ? 1 : displayOrder;
    }

    internal void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;

    public void UpdateDefinition(string definitionEn, string meaningVi, string? descriptionEn = null)
    {
        if (string.IsNullOrWhiteSpace(definitionEn))
            throw new InvalidOperationException("DefinitionEn không được để trống.");
        if (string.IsNullOrWhiteSpace(meaningVi))
            throw new InvalidOperationException("MeaningVi không được để trống.");

        DefinitionEn = definitionEn.Trim();
        MeaningVi = meaningVi.Trim();
        DescriptionEn = string.IsNullOrWhiteSpace(descriptionEn) ? null : descriptionEn.Trim();
    }

    public VocabularyRelationEntity AddRelation(
        string relationType,
        string relatedWord,
        int? displayOrder = null,
        Guid? relatedVocabularyId = null,
        Guid? relatedSenseId = null,
        Guid? id = null)
    {
        int order = displayOrder ?? (_relations.Count == 0 ? 1 : _relations.Max(r => r.DisplayOrder) + 1);
        var relation = new VocabularyRelationEntity(
            senseId: Id,
            relationType: relationType,
            relatedWord: relatedWord,
            displayOrder: order,
            relatedVocabularyId: relatedVocabularyId,
            relatedSenseId: relatedSenseId,
            id: id);

        _relations.Add(relation);
        return relation;
    }

    public void UpdateDetails(string partOfSpeech, string definitionEn, string meaningVi, string cefrLevel, string? descriptionEn)
    {
        UpdateDefinition(definitionEn, meaningVi, descriptionEn);
        PartOfSpeech = string.IsNullOrWhiteSpace(partOfSpeech) ? "noun" : partOfSpeech.Trim().ToLowerInvariant();
        CefrLevel = string.IsNullOrWhiteSpace(cefrLevel) ? "B1" : cefrLevel.Trim().ToUpperInvariant();
    }

    public void ClearRelatedContent()
    {
        _relations.Clear();
        _collocations.Clear();
        _examples.Clear();
        _grammarPatterns.Clear();
    }

    public VocabularyCollocationEntity AddCollocation(
        string phraseEn,
        string? meaningVi = null,
        int? displayOrder = null,
        Guid? id = null)
    {
        int order = displayOrder ?? (_collocations.Count == 0 ? 1 : _collocations.Max(c => c.DisplayOrder) + 1);
        var collocation = new VocabularyCollocationEntity(
            senseId: Id,
            phraseEn: phraseEn,
            meaningVi: meaningVi,
            displayOrder: order,
            id: id);

        _collocations.Add(collocation);
        return collocation;
    }

    public VocabularyExampleEntity AddExample(
        string sentenceEn,
        string? sentenceVi = null,
        string? cefrLevel = null,
        int? displayOrder = null,
        string? audioUrl = null,
        Guid? id = null)
    {
        int order = displayOrder ?? (_examples.Count == 0 ? 1 : _examples.Max(e => e.DisplayOrder) + 1);
        var example = new VocabularyExampleEntity(
            senseId: Id,
            sentenceEn: sentenceEn,
            sentenceVi: sentenceVi,
            cefrLevel: cefrLevel ?? CefrLevel,
            displayOrder: order,
            audioUrl: audioUrl,
            id: id);

        _examples.Add(example);
        return example;
    }

    public VocabularyGrammarPatternEntity AddGrammarPattern(
        string pattern,
        string? explanation = null,
        string? example = null,
        Guid? id = null)
    {
        var grammarPattern = new VocabularyGrammarPatternEntity(
            senseId: Id,
            pattern: pattern,
            explanation: explanation,
            example: example,
            id: id);

        _grammarPatterns.Add(grammarPattern);
        return grammarPattern;
    }
}
