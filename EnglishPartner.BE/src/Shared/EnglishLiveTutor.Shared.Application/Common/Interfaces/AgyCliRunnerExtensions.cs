using System.Diagnostics.CodeAnalysis;
using EnglishLiveTutor.Application.Common.Exceptions;

namespace EnglishLiveTutor.Application.Common.Interfaces;

public static class AgyCliRunnerExtensions
{
    /// <summary>
    /// True when the request targets AGY Local: provider "agy", an "agy:" model prefix, or a model code in the
    /// AGY allowlist. Independent of <see cref="IAgyCliRunner.IsEnabled"/> so a disabled runner still recognises
    /// (and then rejects) AGY requests instead of sending them to Gemini.
    /// </summary>
    public static bool IsAgyRequest(
        this IAgyCliRunner? runner,
        string? provider,
        string? modelCode)
    {
        if (string.Equals(provider?.Trim(), "agy", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(modelCode))
        {
            return false;
        }

        string cleaned = modelCode.Trim();
        if (cleaned.StartsWith("agy:", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return runner?.AllowedModels.Any(
            model => string.Equals(model, cleaned, StringComparison.OrdinalIgnoreCase)) == true;
    }

    public static void EnsureAgyEnabled([NotNull] this IAgyCliRunner? runner)
    {
        if (runner is null)
        {
            throw new InvalidOperationException("Antigravity CLI runner is not configured.");
        }

        if (!runner.IsEnabled)
        {
            throw new ForbiddenException("agy_local_disabled");
        }
    }
}
