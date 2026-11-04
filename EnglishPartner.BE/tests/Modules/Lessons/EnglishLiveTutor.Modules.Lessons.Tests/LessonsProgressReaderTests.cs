using EnglishLiveTutor.Domain.LessonAttempts;
using EnglishLiveTutor.Infrastructure.Persistence.Modules;
using EnglishLiveTutor.Infrastructure.Lessons;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EnglishLiveTutor.Modules.Lessons.Tests;

public class LessonsProgressReaderTests
{
    private static LessonsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LessonsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new LessonsDbContext(options);
    }

    [Fact]
    public async Task GetDailyActivityAsync_Offset420_GroupsDatesAndExcludesOtherUsersAndIncomplete()
    {
        using var db = CreateContext();
        var reader = new LessonsProgressReader(db);

        var targetUserId = "user-lessons-1";
        var otherUserId = "user-lessons-2";

        // UTC: 2026-11-01 16:30:00Z -> Local (+7): 2026-11-01 23:30:00
        var attempt1 = new LessonAttemptEntity(Guid.NewGuid(), targetUserId, 1, 100);
        attempt1.Complete(new DateTimeOffset(2026, 11, 1, 16, 30, 0, TimeSpan.Zero));

        // UTC: 2026-11-01 17:30:00Z -> Local (+7): 2026-11-02 00:30:00
        var attempt2 = new LessonAttemptEntity(Guid.NewGuid(), targetUserId, 1, 100);
        attempt2.Complete(new DateTimeOffset(2026, 11, 1, 17, 30, 0, TimeSpan.Zero));

        // Incomplete attempt (Status=InProgress, CompletedAtUtc=null)
        var incompleteAttempt = new LessonAttemptEntity(Guid.NewGuid(), targetUserId, 1, 100);

        // Other user attempt at 16:30Z
        var otherAttempt = new LessonAttemptEntity(Guid.NewGuid(), otherUserId, 1, 100);
        otherAttempt.Complete(new DateTimeOffset(2026, 11, 1, 16, 30, 0, TimeSpan.Zero));

        // Outside date range
        var outsideAttempt = new LessonAttemptEntity(Guid.NewGuid(), targetUserId, 1, 100);
        outsideAttempt.Complete(new DateTimeOffset(2026, 10, 31, 16, 59, 59, TimeSpan.Zero));

        // Soft-deleted attempt for target user
        var deletedAttempt = new LessonAttemptEntity(Guid.NewGuid(), targetUserId, 1, 100);
        deletedAttempt.Complete(new DateTimeOffset(2026, 11, 1, 16, 45, 0, TimeSpan.Zero));
        deletedAttempt.SoftDelete(DateTimeOffset.UtcNow, targetUserId);

        db.LessonAttempts.AddRange(attempt1, attempt2, incompleteAttempt, otherAttempt, outsideAttempt, deletedAttempt);
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
        var reader = new LessonsProgressReader(db);
        var userId = "user-lesson-summary";
        var otherUserId = "user-lesson-other";

        var attempt1 = new LessonAttemptEntity(Guid.NewGuid(), userId, 1, 0);
        attempt1.Complete(DateTimeOffset.UtcNow.AddDays(-2)); // FinalScore100 = 100

        var attempt2 = new LessonAttemptEntity(Guid.NewGuid(), userId, 1, 0);
        attempt2.Complete(DateTimeOffset.UtcNow.AddDays(-1)); // FinalScore100 = 100

        // User A soft-deleted completed attempt (should not be counted)
        var deletedAttempt = new LessonAttemptEntity(Guid.NewGuid(), userId, 1, 0);
        deletedAttempt.Complete(DateTimeOffset.UtcNow.AddDays(-1));
        deletedAttempt.SoftDelete(DateTimeOffset.UtcNow, userId);

        var incomplete = new LessonAttemptEntity(Guid.NewGuid(), userId, 1, 100);

        // User B completed attempt with different score (0m)
        var otherAttempt = new LessonAttemptEntity(Guid.NewGuid(), otherUserId, 1, 100);
        otherAttempt.Complete(DateTimeOffset.UtcNow.AddDays(-1)); // FinalScore100 = 0

        db.LessonAttempts.AddRange(attempt1, attempt2, deletedAttempt, incomplete, otherAttempt);
        await db.SaveChangesAsync();

        var summary = await reader.GetSummaryAsync(userId, DateTimeOffset.UtcNow.AddDays(-30), CancellationToken.None);

        Assert.Equal(2, summary.CompletedAttempts);
        Assert.NotNull(summary.AverageScore100);
        Assert.Equal(100m, summary.AverageScore100.Value);

        // User B cross-user isolation
        var otherSummary = await reader.GetSummaryAsync(otherUserId, DateTimeOffset.UtcNow.AddDays(-30), CancellationToken.None);
        Assert.Equal(1, otherSummary.CompletedAttempts);
        Assert.NotNull(otherSummary.AverageScore100);
        Assert.Equal(0m, otherSummary.AverageScore100.Value);
    }
}
