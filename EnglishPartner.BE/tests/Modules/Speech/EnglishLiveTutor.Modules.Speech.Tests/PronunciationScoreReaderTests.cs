using EnglishLiveTutor.Application.Features.Pronunciation.Services;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public sealed class PronunciationScoreReaderTests
{
    [Fact]
    public async Task GetLatestScoreAsync_IgnoresNullAndReturnsNewest()
    {
        await using var db = CreateContext();
        var userId = "user-a";
        var senseId = Guid.NewGuid();
        db.PronunciationAssessments.AddRange(
            CreateAssessment(userId, senseId, 65m, DateTimeOffset.UtcNow.AddMinutes(-3)),
            CreateAssessment(userId, senseId, 90m, DateTimeOffset.UtcNow.AddMinutes(-2)),
            CreateAssessment(userId, senseId, null, DateTimeOffset.UtcNow.AddMinutes(-1)),
            CreateAssessment("user-b", senseId, 10m, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();

        var result = await new PronunciationScoreReader(db).GetLatestScoreAsync(userId, senseId);

        Assert.Equal(90m, result);
    }

    [Fact]
    public async Task GetLatestScoresAsync_GroupsBySenseAndIgnoresNull()
    {
        await using var db = CreateContext();
        var userId = "user-a";
        var senseOne = Guid.NewGuid();
        var senseTwo = Guid.NewGuid();
        var senseWithoutScore = Guid.NewGuid();
        db.PronunciationAssessments.AddRange(
            CreateAssessment(userId, senseOne, 70m, DateTimeOffset.UtcNow.AddMinutes(-3)),
            CreateAssessment(userId, senseOne, 80m, DateTimeOffset.UtcNow.AddMinutes(-2)),
            CreateAssessment(userId, senseTwo, 95m, DateTimeOffset.UtcNow.AddMinutes(-2)),
            CreateAssessment(userId, senseWithoutScore, null, DateTimeOffset.UtcNow),
            CreateAssessment("user-b", senseOne, 10m, DateTimeOffset.UtcNow),
            CreateAssessment("user-b", senseTwo, 20m, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();

        var result = await new PronunciationScoreReader(db)
            .GetLatestScoresAsync(userId, [senseOne, senseTwo, senseWithoutScore]);

        Assert.Equal(2, result.Count);
        Assert.Equal(80m, result[senseOne]);
        Assert.Equal(95m, result[senseTwo]);
        Assert.DoesNotContain(senseWithoutScore, result.Keys);
    }

    [Fact]
    public async Task GetSummaryAsync_CountsAllAndAveragesNonNullScores()
    {
        await using var db = CreateContext();
        var userId = "user-a";
        db.PronunciationAssessments.AddRange(
            CreateAssessment(userId, Guid.NewGuid(), 80m, DateTimeOffset.UtcNow.AddMinutes(-3)),
            CreateAssessment(userId, Guid.NewGuid(), 90m, DateTimeOffset.UtcNow.AddMinutes(-2)),
            CreateAssessment(userId, Guid.NewGuid(), null, DateTimeOffset.UtcNow.AddMinutes(-1)),
            CreateAssessment("user-b", Guid.NewGuid(), 10m, DateTimeOffset.UtcNow),
            CreateAssessment("user-b", Guid.NewGuid(), 20m, DateTimeOffset.UtcNow),
            CreateAssessment("user-b", Guid.NewGuid(), 30m, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();

        var result = await new PronunciationScoreReader(db).GetSummaryAsync(userId);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(85m, result.AverageScore);
    }

    private static PronunciationAssessmentEntity CreateAssessment(
        string userId,
        Guid senseId,
        decimal? overallScore,
        DateTimeOffset createdAtUtc)
    {
        var assessment = new PronunciationAssessmentEntity(
            userId,
            Guid.NewGuid(),
            senseId,
            "Word",
            null,
            "word",
            "US",
            "gemini-3.8-flash",
            Guid.NewGuid(),
            1,
            Guid.NewGuid(),
            "word",
            overallScore,
            0.9m,
            "Completed");
        assessment.SetCreationAudit(createdAtUtc);
        return assessment;
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDbContext(options);
    }
}
