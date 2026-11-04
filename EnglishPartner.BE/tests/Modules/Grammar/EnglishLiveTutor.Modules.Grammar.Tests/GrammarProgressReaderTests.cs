using EnglishLiveTutor.Domain.Grammar;
using EnglishLiveTutor.Domain.Grammar.Enums;
using EnglishLiveTutor.Infrastructure.Persistence.Modules;
using EnglishLiveTutor.Infrastructure.Grammar;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EnglishLiveTutor.Modules.Grammar.Tests;

public class GrammarProgressReaderTests
{
    private static GrammarDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<GrammarDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new GrammarDbContext(options);
    }

    [Fact]
    public async Task GetDailyActivityAsync_Offset420_GroupsDatesAndExcludesOtherUsersAndIncomplete()
    {
        using var db = CreateContext();
        var reader = new GrammarProgressReader(db);

        var targetUserId = "user-grammar-1";
        var otherUserId = "user-grammar-2";
        var stepId = Guid.NewGuid();

        var attempt1 = new GrammarAttemptEntity(
            targetUserId, GrammarAttemptKind.StepCore, stepId, "A1",
            [Guid.NewGuid()], new DateTimeOffset(2026, 11, 1, 16, 0, 0, TimeSpan.Zero));
        attempt1.Complete(85m, new DateTimeOffset(2026, 11, 1, 16, 30, 0, TimeSpan.Zero));

        var attempt2 = new GrammarAttemptEntity(
            targetUserId, GrammarAttemptKind.StepCore, stepId, "A1",
            [Guid.NewGuid()], new DateTimeOffset(2026, 11, 1, 17, 0, 0, TimeSpan.Zero));
        attempt2.Complete(90m, new DateTimeOffset(2026, 11, 1, 17, 30, 0, TimeSpan.Zero));

        // Incomplete attempt
        var incomplete = new GrammarAttemptEntity(
            targetUserId, GrammarAttemptKind.StepCore, stepId, "A1",
            [Guid.NewGuid()], new DateTimeOffset(2026, 11, 1, 16, 0, 0, TimeSpan.Zero));

        // Other user
        var otherAttempt = new GrammarAttemptEntity(
            otherUserId, GrammarAttemptKind.StepCore, stepId, "A1",
            [Guid.NewGuid()], new DateTimeOffset(2026, 11, 1, 16, 0, 0, TimeSpan.Zero));
        otherAttempt.Complete(80m, new DateTimeOffset(2026, 11, 1, 16, 30, 0, TimeSpan.Zero));

        // Outside date range
        var outsideAttempt = new GrammarAttemptEntity(
            targetUserId, GrammarAttemptKind.StepCore, stepId, "A1",
            [Guid.NewGuid()], new DateTimeOffset(2026, 10, 31, 16, 0, 0, TimeSpan.Zero));
        outsideAttempt.Complete(80m, new DateTimeOffset(2026, 10, 31, 16, 59, 59, TimeSpan.Zero));

        // Soft-deleted attempt for target user
        var deletedAttempt = new GrammarAttemptEntity(
            targetUserId, GrammarAttemptKind.StepCore, stepId, "A1",
            [Guid.NewGuid()], new DateTimeOffset(2026, 11, 1, 16, 0, 0, TimeSpan.Zero));
        deletedAttempt.Complete(85m, new DateTimeOffset(2026, 11, 1, 16, 45, 0, TimeSpan.Zero));
        deletedAttempt.SoftDelete(DateTimeOffset.UtcNow, targetUserId);

        db.GrammarAttempts.AddRange(attempt1, attempt2, incomplete, otherAttempt, outsideAttempt, deletedAttempt);
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
    public async Task GetSummaryAsync_ComputesLevelsAndCurrentStepCode()
    {
        using var db = CreateContext();
        var reader = new GrammarProgressReader(db);
        var userId = "user-grammar-summary";
        var otherUserId = "user-grammar-other";

        var step1 = new GrammarStepEntity("A1-01", "A1", 1, "Step 1", "Step 1", "Summary", "{}", 1, false, true);
        var step2 = new GrammarStepEntity("A1-02", "A1", 2, "Step 2", "Step 2", "Summary", "{}", 1, false, true);
        db.GrammarSteps.AddRange(step1, step2);

        // User A: Step 1 passed with 85 (Step 2 not passed)
        var progress1 = UserGrammarStepProgressEntity.Create(userId, step1.Id, DateTimeOffset.UtcNow.AddDays(-2));
        progress1.RecordScore(85m, 80m, DateTimeOffset.UtcNow.AddDays(-1));
        db.UserGrammarStepProgress.Add(progress1);

        var attempt = new GrammarAttemptEntity(
            userId, GrammarAttemptKind.StepCore, step1.Id, "A1",
            [Guid.NewGuid()], DateTimeOffset.UtcNow.AddDays(-1));
        attempt.Complete(85m, DateTimeOffset.UtcNow.AddDays(-1));
        db.GrammarAttempts.Add(attempt);

        // User A: Soft-deleted progress on step 2 (must not be counted)
        var deletedProgress = UserGrammarStepProgressEntity.Create(userId, step2.Id, DateTimeOffset.UtcNow.AddDays(-2));
        deletedProgress.RecordScore(95m, 80m, DateTimeOffset.UtcNow.AddDays(-1));
        deletedProgress.SoftDelete(DateTimeOffset.UtcNow, userId);
        db.UserGrammarStepProgress.Add(deletedProgress);

        // User B: Passed step 2 (which User A has not passed)
        var progressOther = UserGrammarStepProgressEntity.Create(otherUserId, step2.Id, DateTimeOffset.UtcNow.AddDays(-2));
        progressOther.RecordScore(90m, 80m, DateTimeOffset.UtcNow.AddDays(-1));
        db.UserGrammarStepProgress.Add(progressOther);

        var attemptOther = new GrammarAttemptEntity(
            otherUserId, GrammarAttemptKind.StepCore, step2.Id, "A1",
            [Guid.NewGuid()], DateTimeOffset.UtcNow.AddDays(-1));
        attemptOther.Complete(90m, DateTimeOffset.UtcNow.AddDays(-1));
        db.GrammarAttempts.Add(attemptOther);

        await db.SaveChangesAsync();

        var summary = await reader.GetSummaryAsync(userId, DateTimeOffset.UtcNow.AddDays(-30), CancellationToken.None);

        Assert.NotNull(summary.Levels);
        var a1Level = summary.Levels.FirstOrDefault(l => l.Level == "A1");
        Assert.NotNull(a1Level);
        Assert.Equal(2, a1Level.TotalSteps);
        Assert.Equal(1, a1Level.PassedSteps);
        Assert.Equal("A1-02", summary.CurrentStepCode);

        // User B isolation check
        var otherSummary = await reader.GetSummaryAsync(otherUserId, DateTimeOffset.UtcNow.AddDays(-30), CancellationToken.None);
        Assert.NotNull(otherSummary.Levels);
        var otherA1 = otherSummary.Levels.FirstOrDefault(l => l.Level == "A1");
        Assert.NotNull(otherA1);
        Assert.Equal(2, otherA1.TotalSteps);
        Assert.Equal(1, otherA1.PassedSteps);
        Assert.Equal("A1-01", otherSummary.CurrentStepCode);
    }
}
