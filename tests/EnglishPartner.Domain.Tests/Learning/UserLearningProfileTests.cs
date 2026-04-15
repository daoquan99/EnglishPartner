using EnglishPartner.Modules.Learning.Domain.UserProfiles;

namespace EnglishPartner.Domain.Tests.Learning;

public sealed class UserLearningProfileTests
{
    private static UserLearningProfile CreateProfile() =>
        UserLearningProfile.Create(Guid.NewGuid(), "Test User");

    // ── Initial state ──────────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldInitialise_WithZeroXpAndA1Level()
    {
        var profile = CreateProfile();

        profile.TotalXp.Should().Be(0);
        profile.CurrentLevel.Should().Be("A1");
        profile.CurrentStreak.Should().Be(0);
    }

    // ── AddXp ─────────────────────────────────────────────────────────────

    [Fact]
    public void AddXp_ShouldIncreaseTotalXp()
    {
        var profile = CreateProfile();

        profile.AddXp(100);

        profile.TotalXp.Should().Be(100);
    }

    [Fact]
    public void AddXp_MultipleCalls_ShouldAccumulate()
    {
        var profile = CreateProfile();

        profile.AddXp(300);
        profile.AddXp(200);

        profile.TotalXp.Should().Be(500);
    }

    [Fact]
    public void AddXp_WithNegativeAmount_ShouldThrowArgumentOutOfRangeException()
    {
        var profile = CreateProfile();

        Action act = () => profile.AddXp(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void AddXp_WithZero_ShouldNotChangeXpOrLevel()
    {
        var profile = CreateProfile();

        profile.AddXp(0);

        profile.TotalXp.Should().Be(0);
        profile.CurrentLevel.Should().Be("A1");
    }

    // ── CEFR Level progression ─────────────────────────────────────────────

    [Theory]
    [InlineData(0,    "A1")]
    [InlineData(499,  "A1")]
    [InlineData(500,  "A2")]
    [InlineData(1499, "A2")]
    [InlineData(1500, "B1")]
    [InlineData(2999, "B1")]
    [InlineData(3000, "B2")]
    [InlineData(4999, "B2")]
    [InlineData(5000, "C1")]
    [InlineData(7499, "C1")]
    [InlineData(7500, "C2")]
    [InlineData(9999, "C2")]
    public void AddXp_ShouldSetCorrectCEFRLevel(int totalXp, string expectedLevel)
    {
        var profile = CreateProfile();

        profile.AddXp(totalXp);

        profile.CurrentLevel.Should().Be(expectedLevel,
            because: $"{totalXp} XP should correspond to level {expectedLevel}");
    }

    [Fact]
    public void AddXp_WhenCrossingThreshold_ShouldLevelUpAutomatically()
    {
        var profile = CreateProfile();
        profile.AddXp(400); // still A1
        profile.CurrentLevel.Should().Be("A1");

        profile.AddXp(100); // 500 total → A2
        profile.CurrentLevel.Should().Be("A2");
    }

    [Fact]
    public void AddXp_WhenCrossing3Thresholds_ShouldReachCorrectLevel()
    {
        var profile = CreateProfile();

        profile.AddXp(7500); // Jump straight to C2

        profile.CurrentLevel.Should().Be("C2");
        profile.TotalXp.Should().Be(7500);
    }
}
