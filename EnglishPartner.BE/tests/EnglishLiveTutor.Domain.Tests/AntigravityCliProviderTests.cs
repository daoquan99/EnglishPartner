using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Infrastructure.Ai;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public sealed class AntigravityCliProviderTests
{
    private readonly AntigravityCliOptions _defaultOptions = new()
    {
        DefaultModel = "gemini-3.8-flash-medium",
        TimeoutSeconds = 60,
        AllowedModels = new List<string>
        {
            "gemini-3.8-flash-medium",
            "gemini-3.8-flash-low",
            "gemini-3.8-flash-high"
        }
    };

    [Fact]
    public void IsAgyModel_ShouldRecognizeAllowedModelsAndAgyPrefix()
    {
        var runner = new AgyCliRunner(Options.Create(_defaultOptions), NullLogger<AgyCliRunner>.Instance);

        Assert.True(runner.IsAgyModel("gemini-3.8-flash-medium"));
        Assert.True(runner.IsAgyModel("gemini-3.8-flash-low"));
        Assert.True(runner.IsAgyModel("gemini-3.8-flash-high"));
        Assert.True(runner.IsAgyModel("agy:custom-anything"));

        Assert.False(runner.IsAgyModel("claude-sonnet-4-6"));
        Assert.False(runner.IsAgyModel("gpt-4o"));
        Assert.False(runner.IsAgyModel(null));
        Assert.False(runner.IsAgyModel(string.Empty));
    }

    [Fact]
    public void NormalizeModel_ShouldReturnAllowedModels_AndRejectClaude()
    {
        var runner = new AgyCliRunner(Options.Create(_defaultOptions), NullLogger<AgyCliRunner>.Instance);

        Assert.Equal("gemini-3.8-flash-high", runner.NormalizeModel("gemini-3.8-flash-high"));
        Assert.Equal("gemini-3.8-flash-low", runner.NormalizeModel("gemini-3.8-flash-low"));
        Assert.Equal("gemini-3.8-flash-medium", runner.NormalizeModel("gemini-3.8-flash-medium"));

        // With agy: prefix
        Assert.Equal("gemini-3.8-flash-medium", runner.NormalizeModel("agy:gemini-3.8-flash-medium"));

        // Claude model must be rejected and fall back to default gemini-3.8-flash-medium
        Assert.Equal("gemini-3.8-flash-medium", runner.NormalizeModel("claude-sonnet-4-6"));
        Assert.Equal("gemini-3.8-flash-medium", runner.NormalizeModel("claude-opus-4-6-thinking"));

        // Null/empty falls back to default
        Assert.Equal("gemini-3.8-flash-medium", runner.NormalizeModel(null));
        Assert.Equal("gemini-3.8-flash-medium", runner.NormalizeModel(""));
    }

    [Fact]
    public void Options_ShouldReadFromConfiguration_Dynamically()
    {
        var customOptions = new AntigravityCliOptions
        {
            DefaultModel = "gemini-3.8-flash-low",
            TimeoutSeconds = 45,
            AllowedModels = new List<string> { "gemini-3.8-flash-low" }
        };

        var runner = new AgyCliRunner(Options.Create(customOptions), NullLogger<AgyCliRunner>.Instance);

        // Since only low is allowed in this custom configuration, high falls back to low
        Assert.Equal("gemini-3.8-flash-low", runner.NormalizeModel("gemini-3.8-flash-high"));
        Assert.Equal("gemini-3.8-flash-low", runner.NormalizeModel("gemini-3.8-flash-low"));
    }
}
