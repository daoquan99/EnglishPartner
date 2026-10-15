using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Learning;

public sealed class VocabularyReviewAttemptEntity : AggregateRoot<Guid>
{
    public string UserId { get; private set; } = "default-learner";
    public Guid VocabularyId { get; private set; }
    public Guid SenseId { get; private set; }
    public string ReviewType { get; private set; } = "FlashcardRecall";
    public string Rating { get; private set; } = "Good"; // Again | Hard | Good | Easy
    public DateTimeOffset ReviewedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? NextReviewAtSnapshot { get; private set; }

    private VocabularyReviewAttemptEntity()
    {
    }

    public VocabularyReviewAttemptEntity(
        string userId,
        Guid vocabularyId,
        Guid senseId,
        string rating,
        DateTimeOffset reviewedAt,
        DateTimeOffset? nextReviewAtSnapshot,
        string reviewType = "FlashcardRecall",
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        UserId = string.IsNullOrWhiteSpace(userId) ? "default-learner" : userId.Trim();
        VocabularyId = vocabularyId;
        SenseId = senseId;
        ReviewType = string.IsNullOrWhiteSpace(reviewType) ? "FlashcardRecall" : reviewType.Trim();
        Rating = string.IsNullOrWhiteSpace(rating) ? "Good" : rating.Trim();
        ReviewedAt = reviewedAt;
        NextReviewAtSnapshot = nextReviewAtSnapshot;
    }
}
