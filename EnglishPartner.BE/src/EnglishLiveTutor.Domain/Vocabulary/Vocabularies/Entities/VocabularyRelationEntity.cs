using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Vocabulary.Vocabularies.Entities;

public sealed class VocabularyRelationEntity : Entity<Guid>
{
    public Guid SenseId { get; private set; }
    public string RelationType { get; private set; } = "Synonym"; // Synonym | Antonym
    public string RelatedWord { get; private set; } = string.Empty;
    public Guid? RelatedVocabularyId { get; private set; }
    public Guid? RelatedSenseId { get; private set; }
    public int DisplayOrder { get; private set; } = 1;

    private VocabularyRelationEntity()
    {
    }

    internal VocabularyRelationEntity(
        Guid senseId,
        string relationType,
        string relatedWord,
        int displayOrder = 1,
        Guid? relatedVocabularyId = null,
        Guid? relatedSenseId = null,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(relatedWord))
            throw new InvalidOperationException("RelatedWord không được để trống.");

        SenseId = senseId;
        RelationType = string.IsNullOrWhiteSpace(relationType) ? "Synonym" : relationType.Trim();
        RelatedWord = relatedWord.Trim();
        RelatedVocabularyId = relatedVocabularyId;
        RelatedSenseId = relatedSenseId;
        DisplayOrder = displayOrder <= 0 ? 1 : displayOrder;
    }
}
