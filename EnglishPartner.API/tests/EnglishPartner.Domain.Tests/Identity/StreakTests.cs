using EnglishPartner.Modules.Identity.Domain;
using EnglishPartner.Modules.Identity.Domain.Users.Entities;

namespace EnglishPartner.Domain.Tests.Identity;

public sealed class StreakTests
{
    private static Streak CreateFresh() => Streak.Create(Guid.NewGuid());

    private static DateOnly Today    => DateOnly.FromDateTime(DateTime.UtcNow);
    private static DateOnly Tomorrow => Today.AddDays(1);

    // ── Initial state ─────────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldInitialiseWithZeroStreak_AndTwoFreezes()
    {
        var streak = CreateFresh();

        streak.CurrentStreak.Should().Be(0);
        streak.LongestStreak.Should().Be(0);
        streak.StreakFreezes.Should().Be(2);
        streak.LastActivityDate.Should().BeNull();
    }

    // ── First check-in ────────────────────────────────────────────────────

    [Fact]
    public void CheckIn_FirstTime_ShouldStartStreakAt1()
    {
        var streak = CreateFresh();

        var result = streak.CheckIn(Today);

        result.IsSuccess.Should().BeTrue();
        streak.CurrentStreak.Should().Be(1);
        streak.LongestStreak.Should().Be(1);
        streak.LastActivityDate.Should().Be(Today);
    }

    // ── Already checked in today ──────────────────────────────────────────

    [Fact]
    public void CheckIn_SameDay_ShouldReturnFailure()
    {
        var streak = CreateFresh();
        streak.CheckIn(Today);

        var result = streak.CheckIn(Today);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(IdentityErrors.Streak.AlreadyCheckedIn);
        streak.CurrentStreak.Should().Be(1, because: "streak should not increase on duplicate check-in");
    }

    // ── Consecutive days ──────────────────────────────────────────────────

    [Fact]
    public void CheckIn_ConsecutiveDay_ShouldIncrementStreak()
    {
        var streak = CreateFresh();
        streak.CheckIn(Today);

        var result = streak.CheckIn(Tomorrow);

        result.IsSuccess.Should().BeTrue();
        streak.CurrentStreak.Should().Be(2);
    }

    [Fact]
    public void CheckIn_ThreeConsecutiveDays_ShouldReachStreak3()
    {
        var streak = CreateFresh();
        streak.CheckIn(Today);
        streak.CheckIn(Today.AddDays(1));
        streak.CheckIn(Today.AddDays(2));

        streak.CurrentStreak.Should().Be(3);
        streak.LongestStreak.Should().Be(3);
    }

    // ── Freeze token ──────────────────────────────────────────────────────

    [Fact]
    public void CheckIn_Gap2Days_WithFreezeAvailable_ShouldMaintainStreak()
    {
        var streak = CreateFresh();
        streak.CheckIn(Today);           // streak = 1, freezes = 2
        var freezesBefore = streak.StreakFreezes;

        streak.CheckIn(Today.AddDays(2)); // gap = 2 days → use freeze

        streak.CurrentStreak.Should().Be(2, because: "freeze token should save the streak");
        streak.StreakFreezes.Should().Be(freezesBefore - 1, because: "one freeze was consumed");
    }

    [Fact]
    public void CheckIn_Gap2Days_WithNoFreezes_ShouldResetStreak()
    {
        var streak = CreateFresh();
        streak.CheckIn(Today);
        streak.CheckIn(Today.AddDays(1));
        // Consume both freezes
        streak.CheckIn(Today.AddDays(3)); // gap 2 → freeze 1 used
        streak.CheckIn(Today.AddDays(5)); // gap 2 → freeze 2 used
        // Now freezes = 0
        var streakBeforeReset = streak.CurrentStreak;
        streak.CheckIn(Today.AddDays(7)); // gap 2 → NO freeze → reset

        streak.CurrentStreak.Should().Be(1, because: "no freeze tokens remain");
        streak.StreakFreezes.Should().Be(0);
    }

    // ── Gap > 2 days — reset ─────────────────────────────────────────────

    [Fact]
    public void CheckIn_Gap3Days_ShouldResetStreak()
    {
        var streak = CreateFresh();
        streak.CheckIn(Today);
        streak.CheckIn(Today.AddDays(1));
        streak.CheckIn(Today.AddDays(2)); // streak = 3

        streak.CheckIn(Today.AddDays(5)); // gap = 3 → reset

        streak.CurrentStreak.Should().Be(1);
    }

    // ── LongestStreak ─────────────────────────────────────────────────────

    [Fact]
    public void CheckIn_LongestStreak_ShouldNotDecreaseAfterReset()
    {
        var streak = CreateFresh();
        streak.CheckIn(Today);
        streak.CheckIn(Today.AddDays(1));
        streak.CheckIn(Today.AddDays(2)); // longest = 3

        streak.CheckIn(Today.AddDays(10)); // reset → current = 1

        streak.CurrentStreak.Should().Be(1);
        streak.LongestStreak.Should().Be(3, because: "longest streak is preserved after reset");
    }

    [Fact]
    public void CheckIn_WhenCurrentExceedsLongest_ShouldUpdateLongest()
    {
        var streak = CreateFresh();
        for (var i = 0; i < 5; i++)
            streak.CheckIn(Today.AddDays(i));

        streak.LongestStreak.Should().Be(5);
        streak.CurrentStreak.Should().Be(5);
    }
}
