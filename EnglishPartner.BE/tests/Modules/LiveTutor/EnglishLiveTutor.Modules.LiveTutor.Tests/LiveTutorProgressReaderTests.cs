using EnglishLiveTutor.Domain.Conversations;
using EnglishLiveTutor.Infrastructure.Persistence.Modules;
using EnglishLiveTutor.Infrastructure.Realtime;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EnglishLiveTutor.Modules.LiveTutor.Tests;

public class LiveTutorProgressReaderTests
{
    private static LiveTutorDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LiveTutorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new LiveTutorDbContext(options);
    }

    [Fact]
    public async Task GetDailyActivityAsync_Offset420_GroupsDatesAndExcludesOtherUsers()
    {
        using var db = CreateContext();
        var reader = new LiveTutorProgressReader(db);

        var targetUserId = "user-livetutor-1";
        var otherUserId = "user-livetutor-2";

        var conv1 = new ConversationEntity(targetUserId, null, "Conv 1");
        conv1.SetCreationAudit(new DateTimeOffset(2026, 11, 1, 16, 30, 0, TimeSpan.Zero));

        var conv2 = new ConversationEntity(targetUserId, null, "Conv 2");
        conv2.SetCreationAudit(new DateTimeOffset(2026, 11, 1, 17, 30, 0, TimeSpan.Zero));

        // Other user
        var otherConv = new ConversationEntity(otherUserId, null, "Other Conv");
        otherConv.SetCreationAudit(new DateTimeOffset(2026, 11, 1, 16, 30, 0, TimeSpan.Zero));

        // Outside date range
        var outsideConv = new ConversationEntity(targetUserId, null, "Outside Conv");
        outsideConv.SetCreationAudit(new DateTimeOffset(2026, 10, 31, 16, 59, 59, TimeSpan.Zero));

        // Soft-deleted conversation for target user (must be excluded)
        var deletedConv = new ConversationEntity(targetUserId, null, "Deleted Conv");
        deletedConv.SetCreationAudit(new DateTimeOffset(2026, 11, 1, 16, 45, 0, TimeSpan.Zero));
        deletedConv.SoftDelete(DateTimeOffset.UtcNow, targetUserId);

        db.Conversations.AddRange(conv1, conv2, otherConv, outsideConv, deletedConv);
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
        var reader = new LiveTutorProgressReader(db);
        var userId = "user-livetutor-summary";
        var otherUserId = "user-livetutor-other";
        var fromUtc = DateTimeOffset.UtcNow.AddDays(-10);

        // User A: 1 old conv, 1 recent conv
        var oldConv = new ConversationEntity(userId, null, "Old Conv");
        oldConv.SetCreationAudit(fromUtc.AddDays(-5));

        var recentConv = new ConversationEntity(userId, null, "Recent Conv");
        recentConv.SetCreationAudit(fromUtc.AddDays(2));

        // User A: Soft-deleted recent conv (must be excluded)
        var deletedConv = new ConversationEntity(userId, null, "Deleted Conv");
        deletedConv.SetCreationAudit(fromUtc.AddDays(3));
        deletedConv.SoftDelete(DateTimeOffset.UtcNow, userId);

        // User B: Conversations both inside and outside range
        var otherOldConv = new ConversationEntity(otherUserId, null, "Other Old Conv");
        otherOldConv.SetCreationAudit(fromUtc.AddDays(-6));

        var otherRecentConv1 = new ConversationEntity(otherUserId, null, "Other Recent Conv 1");
        otherRecentConv1.SetCreationAudit(fromUtc.AddDays(1));

        var otherRecentConv2 = new ConversationEntity(otherUserId, null, "Other Recent Conv 2");
        otherRecentConv2.SetCreationAudit(fromUtc.AddDays(4));

        db.Conversations.AddRange(oldConv, recentConv, deletedConv, otherOldConv, otherRecentConv1, otherRecentConv2);
        await db.SaveChangesAsync();

        var summary = await reader.GetSummaryAsync(userId, fromUtc, CancellationToken.None);

        Assert.Equal(2, summary.Sessions);
        Assert.Equal(1, summary.SessionsSinceFrom);

        // User B cross-user isolation
        var otherSummary = await reader.GetSummaryAsync(otherUserId, fromUtc, CancellationToken.None);
        Assert.Equal(3, otherSummary.Sessions);
        Assert.Equal(2, otherSummary.SessionsSinceFrom);
    }
}
