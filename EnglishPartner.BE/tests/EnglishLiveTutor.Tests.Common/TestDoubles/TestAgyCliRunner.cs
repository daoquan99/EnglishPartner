using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;

namespace EnglishLiveTutor.Domain.Tests.TestDoubles;

public sealed class TestAgyCliRunner : IAgyCliRunner
{
    public static TestAgyCliRunner Disabled { get; } = new(enabled: false);

    public TestAgyCliRunner(
        bool enabled = true,
        string response = "{}",
        IReadOnlyCollection<string>? allowedModels = null)
    {
        IsEnabled = enabled;
        Response = response;
        AllowedModels = allowedModels ??
        [
            "gemini-3.8-flash-medium",
            "gemini-3.8-flash-low",
            "gemini-3.8-flash-high"
        ];
    }

    public bool IsEnabled { get; }
    public IReadOnlyCollection<string> AllowedModels { get; }
    public string Response { get; }
    public List<string> Prompts { get; } = [];

    public bool IsAgyModel(string? modelCode) =>
        IsEnabled &&
        !string.IsNullOrWhiteSpace(modelCode) &&
        (modelCode.Trim().StartsWith("agy:", StringComparison.OrdinalIgnoreCase) ||
         AllowedModels.Contains(modelCode.Trim(), StringComparer.OrdinalIgnoreCase));

    public string NormalizeModel(string? modelCode) =>
        string.IsNullOrWhiteSpace(modelCode) ? "gemini-3.8-flash-medium" : modelCode.Trim();

    public Task<string> ExecutePromptAsync(
        string prompt,
        string? modelCode = null,
        CancellationToken ct = default)
    {
        if (!IsEnabled)
        {
            throw new ForbiddenException("agy_local_disabled");
        }

        Prompts.Add(prompt);
        return Task.FromResult(Response);
    }
}
