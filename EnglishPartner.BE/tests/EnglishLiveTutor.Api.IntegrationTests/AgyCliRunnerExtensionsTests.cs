using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Tests.TestDoubles;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public sealed class AgyCliRunnerExtensionsTests
{
    [Theory]
    [InlineData("agy", "gemini-3.8-flash", true)]
    [InlineData(" AGY ", null, true)]
    [InlineData("gemini", "gemini-3.8-flash-medium", true)] // AGY model code under a non-AGY provider
    [InlineData(null, "GEMINI-3.8-FLASH-HIGH", true)]
    [InlineData("gemini", "agy:anything", true)]
    [InlineData("gemini", "gemini-3.8-flash", false)]
    [InlineData(null, "gemini-3.5-flash-lite", false)]
    [InlineData("gemini", "future-model-high", false)] // no suffix heuristic: only the allowlist counts
    [InlineData(null, null, false)]
    public void IsAgyRequest_RecognisesAgyRegardlessOfEnabledState(string? provider, string? modelCode, bool expected)
    {
        Assert.Equal(expected, TestAgyCliRunner.Disabled.IsAgyRequest(provider, modelCode));
        Assert.Equal(expected, new TestAgyCliRunner(enabled: true).IsAgyRequest(provider, modelCode));
    }

    [Fact]
    public void EnsureAgyEnabled_WhenDisabled_ThrowsForbidden()
    {
        var ex = Assert.Throws<ForbiddenException>(() => TestAgyCliRunner.Disabled.EnsureAgyEnabled());
        Assert.Equal("agy_local_disabled", ex.Message);
    }

    [Fact]
    public void EnsureAgyEnabled_WhenEnabled_DoesNotThrow()
    {
        new TestAgyCliRunner(enabled: true).EnsureAgyEnabled();
    }
}
