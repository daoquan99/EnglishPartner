using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Infrastructure.Persistence.Modules;
using EnglishLiveTutor.Infrastructure.Pronunciation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EnglishLiveTutor.Modules.Speech.Tests;

public class SpeechProgressReaderTests
{
    private static SpeechDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SpeechDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new SpeechDbContext(options);
    }

    [Fact]
    public async Task GetDailyActivityAsync_Offset420_GroupsDatesAndExcludesOtherUsers()
    {
        using var db = CreateContext();
        var reader = new SpeechProgressReader(db);

        var targetUserId = "user-speech-1";
        var otherUserId = "user-speech-2";

        var a1 = new PronunciationAssessmentEntity(
            targetUserId, Guid.NewGuid(), Guid.NewGuid(), "Word", null, "test", "US", "gemini",
            Guid.NewGuid(), 1, Guid.NewGuid(), "test", 80m, 0.9m, "Completed");
        a1.SetCreationAudit(new DateTimeOffset(2026, 11, 1, 16, 30, 0, TimeSpan.Zero));

        var a2 = new PronunciationAssessmentEntity(
            targetUserId, Guid.NewGuid(), Guid.NewGuid(), "Word", null, "test", "US", "gemini",
            Guid.NewGuid(), 1, Guid.NewGuid(), "test", 90m, 0.9m, "Completed");
        a2.SetCreationAudit(new DateTimeOffset(2026, 11, 1, 17, 30, 0, TimeSpan.Zero));

        // Other user
        var otherAssessment = new PronunciationAssessmentEntity(
            otherUserId, Guid.NewGuid(), Guid.NewGuid(), "Word", null, "test", "US", "gemini",
            Guid.NewGuid(), 1, Guid.NewGuid(), "test", 85m, 0.9m, "Completed");
        otherAssessment.SetCreationAudit(new DateTimeOffset(2026, 11, 1, 16, 30, 0, TimeSpan.Zero));

        // Outside date range
        var outsideAssessment = new PronunciationAssessmentEntity(
            targetUserId, Guid.NewGuid(), Guid.NewGuid(), "Word", null, "test", "US", "gemini",
            Guid.NewGuid(), 1, Guid.NewGuid(), "test", 70m, 0.9m, "Completed");
        outsideAssessment.SetCreationAudit(new DateTimeOffset(2026, 10, 31, 16, 59, 59, TimeSpan.Zero));

        // Soft-deleted assessment for target user (must be excluded)
        var deletedAssessment = new PronunciationAssessmentEntity(
            targetUserId, Guid.NewGuid(), Guid.NewGuid(), "Word", null, "test", "US", "gemini",
            Guid.NewGuid(), 1, Guid.NewGuid(), "test", 85m, 0.9m, "Completed");
        deletedAssessment.SetCreationAudit(new DateTimeOffset(2026, 11, 1, 16, 45, 0, TimeSpan.Zero));
        deletedAssessment.SoftDelete(DateTimeOffset.UtcNow, targetUserId);

        db.PronunciationAssessments.AddRange(a1, a2, otherAssessment, outsideAssessment, deletedAssessment);
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
        var reader = new SpeechProgressReader(db);
        var userId = "user-speech-summary";
        var otherUserId = "user-speech-other";
        var fromUtc = DateTimeOffset.UtcNow.AddDays(-10);

        // User A: 1 old assessment (70m), 1 recent assessment (90m)
        var oldAssessment = new PronunciationAssessmentEntity(
            userId, Guid.NewGuid(), Guid.NewGuid(), "Word", null, "test", "US", "gemini",
            Guid.NewGuid(), 1, Guid.NewGuid(), "test", 70m, 0.9m, "Completed");
        oldAssessment.SetCreationAudit(fromUtc.AddDays(-5));

        var recentAssessment = new PronunciationAssessmentEntity(
            userId, Guid.NewGuid(), Guid.NewGuid(), "Word", null, "test", "US", "gemini",
            Guid.NewGuid(), 1, Guid.NewGuid(), "test", 90m, 0.9m, "Completed");
        recentAssessment.SetCreationAudit(fromUtc.AddDays(2));

        // User A: Soft-deleted recent assessment (10m, must not be counted)
        var deletedAssessment = new PronunciationAssessmentEntity(
            userId, Guid.NewGuid(), Guid.NewGuid(), "Word", null, "test", "US", "gemini",
            Guid.NewGuid(), 1, Guid.NewGuid(), "test", 10m, 0.9m, "Completed");
        deletedAssessment.SetCreationAudit(fromUtc.AddDays(3));
        deletedAssessment.SoftDelete(DateTimeOffset.UtcNow, userId);

        // User B: Old and recent assessments with different scores (40m, 50m)
        var otherOldAssessment = new PronunciationAssessmentEntity(
            otherUserId, Guid.NewGuid(), Guid.NewGuid(), "Word", null, "test", "US", "gemini",
            Guid.NewGuid(), 1, Guid.NewGuid(), "test", 40m, 0.9m, "Completed");
        otherOldAssessment.SetCreationAudit(fromUtc.AddDays(-6));

        var otherRecentAssessment = new PronunciationAssessmentEntity(
            otherUserId, Guid.NewGuid(), Guid.NewGuid(), "Word", null, "test", "US", "gemini",
            Guid.NewGuid(), 1, Guid.NewGuid(), "test", 50m, 0.9m, "Completed");
        otherRecentAssessment.SetCreationAudit(fromUtc.AddDays(4));

        db.PronunciationAssessments.AddRange(oldAssessment, recentAssessment, deletedAssessment, otherOldAssessment, otherRecentAssessment);
        await db.SaveChangesAsync();

        var summary = await reader.GetSummaryAsync(userId, fromUtc, CancellationToken.None);

        Assert.Equal(2, summary.Assessments);
        Assert.NotNull(summary.AverageScore);
        Assert.Equal(80m, summary.AverageScore.Value);
        Assert.NotNull(summary.AverageScoreSinceFrom);
        Assert.Equal(90m, summary.AverageScoreSinceFrom.Value);

        // User B cross-user isolation
        var otherSummary = await reader.GetSummaryAsync(otherUserId, fromUtc, CancellationToken.None);
        Assert.Equal(2, otherSummary.Assessments);
        Assert.NotNull(otherSummary.AverageScore);
        Assert.Equal(45m, otherSummary.AverageScore.Value);
        Assert.NotNull(otherSummary.AverageScoreSinceFrom);
        Assert.Equal(50m, otherSummary.AverageScoreSinceFrom.Value);
    }
}
