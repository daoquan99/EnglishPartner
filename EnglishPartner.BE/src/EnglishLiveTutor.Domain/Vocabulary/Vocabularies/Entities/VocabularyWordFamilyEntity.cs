using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Vocabulary.Vocabularies.Entities;

public sealed class VocabularyWordFamilyEntity : Entity<Guid>
{
    public Guid VocabularyId { get; private set; }
    public string RelatedWord { get; private set; } = string.Empty;
    public string PartOfSpeech { get; private set; } = "noun";
    public Guid? RelatedVocabularyId { get; private set; }

    private VocabularyWordFamilyEntity()
    {
    }

    internal VocabularyWordFamilyEntity(
        Guid vocabularyId,
        string relatedWord,
        string partOfSpeech = "noun",
        Guid? relatedVocabularyId = null,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(relatedWord))
            throw new InvalidOperationException("RelatedWord không được để trống.");

        VocabularyId = vocabularyId;
        RelatedWord = relatedWord.Trim();
        PartOfSpeech = string.IsNullOrWhiteSpace(partOfSpeech) ? "noun" : partOfSpeech.Trim();
        RelatedVocabularyId = relatedVocabularyId;
    }
}
