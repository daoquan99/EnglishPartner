using EnglishPartner.SharedKernel.Domain;

namespace EnglishPartner.Modules.Curriculum.Domain.Vocabularies.Entities;

/// <summary>
/// A synonym or closely related word for a vocabulary entry.
/// </summary>
public sealed class VocabularySynonym : Entity
{
    public Guid VocabularyId { get; private set; }
    public string SynonymWord { get; private set; } = null!;

    private VocabularySynonym() { }

    public static VocabularySynonym Create(Guid vocabularyId, string synonymWord)
    {
        return new VocabularySynonym
        {
            Id = Guid.NewGuid(),
            VocabularyId = vocabularyId,
            SynonymWord = synonymWord.Trim()
        };
    }
}
