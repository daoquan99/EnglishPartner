using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Vocabulary.Vocabularies.Entities;

public sealed class VocabularyCollocationEntity : Entity<Guid>
{
    public Guid SenseId { get; private set; }
    public string PhraseEn { get; private set; } = string.Empty;
    public string? MeaningVi { get; private set; }
    public int DisplayOrder { get; private set; } = 1;

    private VocabularyCollocationEntity()
    {
    }

    internal VocabularyCollocationEntity(
        Guid senseId,
        string phraseEn,
        string? meaningVi = null,
        int displayOrder = 1,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(phraseEn))
            throw new InvalidOperationException("PhraseEn không được để trống.");

        SenseId = senseId;
        PhraseEn = phraseEn.Trim();
        MeaningVi = string.IsNullOrWhiteSpace(meaningVi) ? null : meaningVi.Trim();
        DisplayOrder = displayOrder <= 0 ? 1 : displayOrder;
    }
}
