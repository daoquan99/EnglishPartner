using EnglishPartner.Modules.Learning.Domain.ItemMasteries;

namespace EnglishPartner.Domain.Tests.Learning;

public sealed class ItemMasteryTests
{
    // ── Factories ──────────────────────────────────────────────────────────

    private static ItemMastery CreateNew() =>
        ItemMastery.Create(Guid.NewGuid(), Guid.NewGuid(), ItemType.Vocabulary);

    // ── Initial state ──────────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldInitialise_WithDefaultSM2Values()
    {
        var mastery = CreateNew();

        mastery.EasinessFactor.Should().Be(2.5);
        mastery.IntervalDays.Should().Be(1);
        mastery.Repetitions.Should().Be(0);
        mastery.NextReviewDate.Should().Be(DateTime.UtcNow.Date);
    }

    // ── Quality < 3 (failure) ─────────────────────────────────────────────

    [Theory]
    [InlineData(0)] // Blackout
    [InlineData(1)] // Completely wrong
    [InlineData(2)] // Incorrect but familiar
    public void Review_WhenQualityLessThan3_ShouldResetRepetitionsAndInterval(int quality)
    {
        var mastery = CreateNew();
        // Give it some history first
        mastery.Review(4, DateTime.UtcNow);
        mastery.Review(4, DateTime.UtcNow);

        mastery.Review(quality, DateTime.UtcNow);

        mastery.Repetitions.Should().Be(0);
        mastery.IntervalDays.Should().Be(1);
    }

    // ── Quality ≥ 3 (success) progressions ────────────────────────────────

    [Fact]
    public void Review_FirstSuccessfulReview_ShouldSetInterval1_AndReps1()
    {
        var mastery = CreateNew(); // Reps = 0

        mastery.Review(4, DateTime.UtcNow);

        mastery.Repetitions.Should().Be(1);
        mastery.IntervalDays.Should().Be(1);
    }

    [Fact]
    public void Review_SecondSuccessfulReview_ShouldSetInterval6_AndReps2()
    {
        var mastery = CreateNew();
        mastery.Review(4, DateTime.UtcNow); // Reps → 1, Interval → 1

        mastery.Review(4, DateTime.UtcNow); // Reps → 2, Interval → 6

        mastery.Repetitions.Should().Be(2);
        mastery.IntervalDays.Should().Be(6);
    }

    [Fact]
    public void Review_ThirdSuccessfulReview_ShouldMultiplyIntervalByEF()
    {
        var mastery = CreateNew();
        mastery.Review(4, DateTime.UtcNow); // rep 0→1, interval 1
        mastery.Review(4, DateTime.UtcNow); // rep 1→2, interval 6

        mastery.Review(4, DateTime.UtcNow); // rep 2→3, interval = round(6 × EF)

        var expectedInterval = (int)Math.Round(6 * mastery.EasinessFactor / 1); // before this review EF was already updated
        mastery.Repetitions.Should().Be(3);
        mastery.IntervalDays.Should().BeGreaterThan(6);
    }

    // ── EasinessFactor updates ────────────────────────────────────────────

    [Fact]
    public void Review_WithPerfectScore_ShouldIncreaseEF()
    {
        var mastery = CreateNew();
        var efBefore = mastery.EasinessFactor;

        mastery.Review(5, DateTime.UtcNow);

        mastery.EasinessFactor.Should().BeGreaterThan(efBefore);
    }

    [Fact]
    public void Review_WithLowPassScore_ShouldDecreaseEF()
    {
        var mastery = CreateNew();
        var efBefore = mastery.EasinessFactor;

        mastery.Review(3, DateTime.UtcNow); // Pass but lowest

        mastery.EasinessFactor.Should().BeLessThan(efBefore);
    }

    [Fact]
    public void Review_EFNeverDropsBelow1Point3()
    {
        var mastery = CreateNew();

        // Apply many failing reviews — EF should never go below 1.3
        for (var i = 0; i < 20; i++)
            mastery.Review(0, DateTime.UtcNow);

        mastery.EasinessFactor.Should().BeGreaterThanOrEqualTo(1.3);
    }

    // ── NextReviewDate ────────────────────────────────────────────────────

    [Fact]
    public void Review_ShouldSetNextReviewDate_ToTodayPlusInterval()
    {
        var mastery  = CreateNew();
        var today    = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        mastery.Review(4, today);
        var interval = mastery.IntervalDays;

        mastery.NextReviewDate.Should().Be(today.Date.AddDays(interval));
    }

    // ── Guard clauses ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(100)]
    [InlineData(-99)]
    public void Review_WithInvalidQuality_ShouldThrowArgumentOutOfRangeException(int quality)
    {
        var mastery = CreateNew();

        Action act = () => mastery.Review(quality, DateTime.UtcNow);

        act.Should().Throw<ArgumentOutOfRangeException>()
           .WithParameterName("quality");
    }

    // ── Idempotency / multiple reviews ───────────────────────────────────

    [Fact]
    public void Review_MultipleConsecutiveSuccesses_ShouldProgressIntervalGeometrically()
    {
        var mastery = CreateNew();
        var today   = DateTime.UtcNow;

        mastery.Review(5, today); // rep 1, interval 1
        mastery.Review(5, today); // rep 2, interval 6
        mastery.Review(5, today); // rep 3, interval > 6

        mastery.IntervalDays.Should().BeGreaterThan(6);
        mastery.Repetitions.Should().Be(3);
    }
}
