using EnglishLiveTutor.Domain.Learning;
using EnglishLiveTutor.Infrastructure.Persistence.Modules;
using EnglishLiveTutor.Infrastructure.Learning;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EnglishLiveTutor.Modules.Vocabulary.Tests;

public class VocabularyProgressReaderTests
{
    private static VocabularyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<VocabularyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new VocabularyDbContext(options);
    }

    [Fact]
    public async Task GetDailyActivityAsync_Offset420_GroupsDatesAndExcludesOtherUsersAndOutsideRange()
    {
        using var db = CreateContext();
        var reader = new VocabularyProgressReader(db);

        var targetUserId = "user-123";
        var otherUserId = "user-999";

        // UTC: 2026-11-01 16:30:00Z -> Local (+7): 2026-11-01 23:30:00
        var attempt1 = new VocabularyReviewAttemptEntity(
            targetUserId, Guid.NewGuid(), Guid.NewGuid(), "Good",
            new DateTimeOffset(2026, 11, 1, 16, 30, 0, TimeSpan.Zero), null);

        // UTC: 2026-11-01 17:30:00Z -> Local (+7): 2026-11-02 00:30:00
        var attempt2 = new VocabularyReviewAttemptEntity(
            targetUserId, Guid.NewGuid(), Guid.NewGuid(), "Good",
            new DateTimeOffset(2026, 11, 1, 17, 30, 0, TimeSpan.Zero), null);

        // Other user at same time
        var otherAttempt = new VocabularyReviewAttemptEntity(
            otherUserId, Guid.NewGuid(), Guid.NewGuid(), "Good",
            new DateTimeOffset(2026, 11, 1, 16, 30, 0, TimeSpan.Zero), null);

        // Outside range (before)
        var outsideAttempt = new VocabularyReviewAttemptEntity(
            targetUserId, Guid.NewGuid(), Guid.NewGuid(), "Good",
            new DateTimeOffset(2026, 10, 31, 16, 59, 59, TimeSpan.Zero), null);

        // Soft-deleted attempt for target user (must be excluded)
        var deletedAttempt = new VocabularyReviewAttemptEntity(
            targetUserId, Guid.NewGuid(), Guid.NewGuid(), "Good",
            new DateTimeOffset(2026, 11, 1, 16, 45, 0, TimeSpan.Zero), null);
        deletedAttempt.SoftDelete(DateTimeOffset.UtcNow, targetUserId);

        db.VocabularyReviewAttempts.AddRange(attempt1, attempt2, otherAttempt, outsideAttempt, deletedAttempt);
        await db.SaveChangesAsync();

        var result = await reader.GetDailyActivityAsync(
            targetUserId,
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 2),
            utcOffsetMinutes: 420,
            CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, a => a.Date == new DateOnly(2026, 11, 1) && a.Count == 1);
        Assert.Contains(result, a => a.Date == new DateOnly(2026, 11, 2) && a.Count == 1);
    }

    [Fact]
    public async Task GetSummaryAsync_ComputesCorrectMetrics()
    {
        using var db = CreateContext();
        var reader = new VocabularyProgressReader(db);
        var userId = "user-vocab-summary";
        var otherUserId = "other-user";
        var nowUtc = DateTimeOffset.UtcNow;

        var p1 = new UserVocabularyProgressEntity(
            userId, Guid.NewGuid(), Guid.NewGuid(),
            masteryLevel: 80,
            learningStatus: "Mastered");
        p1.ApplyReview("easy", nowUtc); // NextReview in future (+5 days). Mastery = 100, Mastered

        var p2 = new UserVocabularyProgressEntity(
            userId, Guid.NewGuid(), Guid.NewGuid(),
            masteryLevel: 40,
            learningStatus: "Learning");
        p2.ApplyReview("again", nowUtc.AddDays(-2)); // NextReview in past (-44 hours, due!). Mastery = 25, Learning

        // Soft-deleted progress for user A (must be excluded)
        var deletedProgress = new UserVocabularyProgressEntity(
            userId, Guid.NewGuid(), Guid.NewGuid(),
            masteryLevel: 50,
            learningStatus: "Learning");
        deletedProgress.SoftDelete(DateTimeOffset.UtcNow, userId);

        var otherUserProgress = new UserVocabularyProgressEntity(
            otherUserId, Guid.NewGuid(), Guid.NewGuid(),
            masteryLevel: 100,
            learningStatus: "Mastered");

        db.UserVocabularyProgresses.AddRange(p1, p2, deletedProgress, otherUserProgress);
        await db.SaveChangesAsync();

        var summary = await reader.GetSummaryAsync(userId, nowUtc.AddDays(-30), CancellationToken.None);

        Assert.Equal(2, summary.Studied);
        Assert.Equal(1, summary.Mastered);
        Assert.Equal(1, summary.DueForReviewNow);
        Assert.Equal(62.5, summary.AverageMastery);

        var otherSummary = await reader.GetSummaryAsync(otherUserId, nowUtc.AddDays(-30), CancellationToken.None);
        Assert.Equal(1, otherSummary.Studied);
        Assert.Equal(1, otherSummary.Mastered);
        Assert.Equal(0, otherSummary.DueForReviewNow);
        Assert.Equal(100.0, otherSummary.AverageMastery);
    }
}
