namespace EnglishPartner.Modules.AI.Contracts.Services;

/// <summary>
/// Module Interface — allows other modules (e.g. Learning) to request AI evaluation
/// of a user's answer without coupling to the AI Application or Infrastructure layers.
/// Per architecture rules: cross-module sync calls use Module Interfaces in Contracts.
/// </summary>
public interface IAiEvaluationService
{
    /// <summary>
    /// Evaluates a user's text answer against a curriculum item using Gemini AI.
    /// Returns null if the item is not found or if AI evaluation fails (use fallback score).
    /// </summary>
    /// <param name="itemId">The curriculum item ID to evaluate against.</param>
    /// <param name="itemType">Learning.Domain.ItemMasteries.ItemType numeric value.</param>
    /// <param name="userAnswer">The user's text answer to evaluate.</param>
    Task<AiEvaluationResult?> EvaluateAsync(
        Guid itemId,
        int itemType,
        string userAnswer,
        CancellationToken cancellationToken = default);
}

public sealed record AiEvaluationResult(
    int QualityScore,           // 0-5 SM-2 compatible scale
    bool IsCorrect,
    string? SuggestedCorrection,
    string? Explanation);
