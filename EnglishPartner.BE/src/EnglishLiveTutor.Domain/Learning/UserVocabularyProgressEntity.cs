using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Learning;

public sealed class UserVocabularyProgressEntity : AggregateRoot<Guid>
{
    public string UserId { get; private set; } = "default-learner";
    public Guid VocabularyId { get; private set; }
    public Guid SenseId { get; private set; }
    public int MasteryLevel { get; private set; } // 0..100
    public int CorrectCount { get; private set; }
    public int IncorrectCount { get; private set; }
    public int ReviewCount { get; private set; }
    public DateTimeOffset? LastReviewedAt { get; private set; }
    public DateTimeOffset? NextReviewAt { get; private set; }
    public int IntervalDays { get; private set; }
    public decimal EaseFactor { get; private set; } = 2.50m;
    public string LearningStatus { get; private set; } = "New"; // New | Learning | Review | Mastered
    public decimal? LatestPronunciationScore { get; private set; }

    private UserVocabularyProgressEntity()
    {
    }

    public UserVocabularyProgressEntity(
        string userId,
        Guid vocabularyId,
        Guid senseId,
        int masteryLevel = 0,
        int intervalDays = 0,
        decimal easeFactor = 2.50m,
        string learningStatus = "New",
        int reviewCount = 0,
        int correctCount = 0,
        int incorrectCount = 0,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        UserId = string.IsNullOrWhiteSpace(userId) ? "default-learner" : userId.Trim();
        VocabularyId = vocabularyId;
        SenseId = senseId;
        MasteryLevel = Math.Clamp(masteryLevel, 0, 100);
        IntervalDays = Math.Max(0, intervalDays);
        EaseFactor = easeFactor <= 0 ? 2.50m : easeFactor;
        LearningStatus = string.IsNullOrWhiteSpace(learningStatus) ? "New" : learningStatus.Trim();
        ReviewCount = Math.Max(0, reviewCount);
        CorrectCount = Math.Max(0, correctCount);
        IncorrectCount = Math.Max(0, incorrectCount);
    }

    public string ApplyReview(string? ratingRaw, DateTimeOffset now)
    {
        string rating = (ratingRaw ?? "Good").Trim();
        ReviewCount += 1;
        LastReviewedAt = now;

        switch (rating.ToLowerInvariant())
        {
            case "again":
                rating = "Again";
                IncorrectCount += 1;
                IntervalDays = 1;
                EaseFactor = Math.Max(1.30m, EaseFactor - 0.20m);
                MasteryLevel = Math.Max(0, MasteryLevel - 15);
                LearningStatus = "Learning";
                NextReviewAt = now.AddHours(4);
                break;

            case "hard":
                rating = "Hard";
                CorrectCount += 1;
                IntervalDays = Math.Max(1, (int)Math.Round(Math.Max(1, IntervalDays) * 1.2m));
                EaseFactor = Math.Max(1.30m, EaseFactor - 0.15m);
                MasteryLevel = Math.Min(100, MasteryLevel + 12);
                LearningStatus = "Review";
                NextReviewAt = now.AddDays(IntervalDays);
                break;

            case "easy":
                rating = "Easy";
                CorrectCount += 1;
                IntervalDays = IntervalDays <= 1
                    ? 5
                    : (int)Math.Round(IntervalDays * EaseFactor * 1.3m);
                EaseFactor = Math.Min(3.00m, EaseFactor + 0.15m);
                MasteryLevel = Math.Min(100, MasteryLevel + 30);
                LearningStatus = MasteryLevel >= 80 ? "Mastered" : "Review";
                NextReviewAt = now.AddDays(IntervalDays);
                break;

            default:
                rating = "Good";
                CorrectCount += 1;
                IntervalDays = IntervalDays <= 1
                    ? 3
                    : (int)Math.Round(IntervalDays * EaseFactor);
                MasteryLevel = Math.Min(100, MasteryLevel + 22);
                LearningStatus = MasteryLevel >= 85 ? "Mastered" : "Review";
                NextReviewAt = now.AddDays(IntervalDays);
                break;
        }

        return rating;
    }

    public void UpdateLatestPronunciationScore(decimal score)
    {
        LatestPronunciationScore = Math.Clamp(score, 0m, 100m);
    }
}
