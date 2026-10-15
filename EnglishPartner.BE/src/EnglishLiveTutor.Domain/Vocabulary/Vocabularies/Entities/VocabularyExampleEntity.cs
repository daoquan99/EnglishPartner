using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Vocabulary.Vocabularies.Entities;

public sealed class VocabularyExampleEntity : Entity<Guid>
{
    public Guid SenseId { get; private set; }
    public string SentenceEn { get; private set; } = string.Empty;
    public string? SentenceVi { get; private set; }
    public string? AudioUrl { get; private set; }
    public string? CefrLevel { get; private set; }
    public int DisplayOrder { get; private set; } = 1;

    private VocabularyExampleEntity()
    {
    }

    internal VocabularyExampleEntity(
        Guid senseId,
        string sentenceEn,
        string? sentenceVi = null,
        string? cefrLevel = null,
        int displayOrder = 1,
        string? audioUrl = null,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(sentenceEn))
            throw new InvalidOperationException("SentenceEn không được để trống.");

        SenseId = senseId;
        SentenceEn = sentenceEn.Trim();
        SentenceVi = string.IsNullOrWhiteSpace(sentenceVi) ? null : sentenceVi.Trim();
        CefrLevel = string.IsNullOrWhiteSpace(cefrLevel) ? null : cefrLevel.Trim().ToUpperInvariant();
        DisplayOrder = displayOrder <= 0 ? 1 : displayOrder;
        AudioUrl = string.IsNullOrWhiteSpace(audioUrl) ? null : audioUrl.Trim();
    }
}
