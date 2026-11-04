using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Api.Configuration;
using EnglishLiveTutor.Infrastructure.Ai;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EnglishLiveTutor.Domain.Tests;

public sealed class AntigravityCliProviderTests
{
    private readonly AntigravityCliOptions _defaultOptions = new()
    {
        Enabled = true,
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
    public void Options_DefaultDisabled()
    {
        var options = new AntigravityCliOptions();

        Assert.False(options.Enabled);
    }

    [Fact]
    public async Task DisabledRunner_RejectsAgyModelsAndExecution()
    {
        var disabledOptions = new AntigravityCliOptions
        {
            Enabled = false,
            AllowedModels = ["gemini-3.8-flash-medium"]
        };
        var runner = new AgyCliRunner(Options.Create(disabledOptions), NullLogger<AgyCliRunner>.Instance);

        Assert.False(runner.IsAgyModel("gemini-3.8-flash-medium"));
        await Assert.ThrowsAsync<ForbiddenException>(
            () => runner.ExecutePromptAsync("test prompt"));
    }

    [Fact]
    public void StartupValidator_EnabledOutsideDevelopment_Throws()
    {
        var environment = new TestHostEnvironment("Testing");
        var options = new AntigravityCliOptions { Enabled = true };

        var error = Assert.Throws<InvalidOperationException>(
            () => AntigravityCliStartupValidator.Validate(environment, options));

        Assert.Contains("only allowed in the Development environment", error.Message);
        Assert.DoesNotContain("secret", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StartupValidator_EnabledInDevelopment_DoesNotThrow()
    {
        var environment = new TestHostEnvironment("Development");
        var options = new AntigravityCliOptions { Enabled = true };

        AntigravityCliStartupValidator.Validate(environment, options);
    }

    [Fact]
    public void Program_EnabledOutsideDevelopment_FailsHostStartup()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:SecretKey"] = "test-jwt-secret-key-that-is-at-least-32-bytes-long!",
                        ["Database:MigrateOnStartup"] = "false",
                        ["Database:SeedOnStartup"] = "false",
                        ["Ai:AntigravityCli:Enabled"] = "true"
                    });
                });
            });

        var error = Record.Exception(() => factory.CreateClient());

        Assert.NotNull(error);
        Assert.Contains("Ai:AntigravityCli:Enabled is only allowed in the Development environment.", error.ToString());
    }

    [Fact]
    public void BuildProcessStartInfo_SandboxesWorkingDirectoryAndEnvironment()
    {
        string tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "englishpartner-agy-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        const string connectionVariable = "ConnectionStrings__DefaultConnection";
        const string jwtVariable = "Jwt__SecretKey";
        string? originalConnection = Environment.GetEnvironmentVariable(connectionVariable);
        string? originalJwt = Environment.GetEnvironmentVariable(jwtVariable);

        try
        {
            Environment.SetEnvironmentVariable(connectionVariable, "server=must-not-leak");
            Environment.SetEnvironmentVariable(jwtVariable, "must-not-leak");

            var psi = AgyCliRunner.BuildProcessStartInfo(
                @"C:\tools\agy.exe",
                "gemini-3.8-flash-medium",
                "test prompt",
                tempDirectory);

            Assert.Equal(tempDirectory, psi.WorkingDirectory);
            Assert.NotEqual(Directory.GetCurrentDirectory(), psi.WorkingDirectory);
            Assert.Contains("--sandbox", psi.ArgumentList);
            Assert.Contains("--settings", psi.ArgumentList);
            Assert.Contains(
                """{"allowNonWorkspaceAccess":false,"permissions":{"allow":[],"deny":["read_file(*)","write_file(*)","command(*)","mcp(*)","read_url(*)"]}}""",
                psi.ArgumentList);
            Assert.False(psi.Environment.ContainsKey(connectionVariable));
            Assert.False(psi.Environment.ContainsKey(jwtVariable));
        }
        finally
        {
            Environment.SetEnvironmentVariable(connectionVariable, originalConnection);
            Environment.SetEnvironmentVariable(jwtVariable, originalJwt);
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

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

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "EnglishLiveTutor.Api.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
