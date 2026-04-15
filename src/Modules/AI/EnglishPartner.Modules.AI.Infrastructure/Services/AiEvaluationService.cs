using EnglishPartner.Modules.AI.Application.Abstractions;
using EnglishPartner.Modules.AI.Contracts.Services;
using EnglishPartner.Modules.Curriculum.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace EnglishPartner.Modules.AI.Infrastructure.Services;

/// <summary>
/// Implements IAiEvaluationService by fetching curriculum context and calling GeminiService.
/// ItemType values follow Learning.Domain.ItemMasteries.ItemType — mapped explicitly to CurriculumItemType.
/// </summary>
internal sealed class AiEvaluationService(
    IGeminiService geminiService,
    ICurriculumQueryService curriculumQueryService,
    ILogger<AiEvaluationService> logger) : IAiEvaluationService
{
    public async Task<AiEvaluationResult?> EvaluateAsync(
        Guid itemId, int itemType, string userAnswer, CancellationToken ct = default)
    {
        try
        {
            // Map Learning.ItemType numeric values → CurriculumItemType
            // Learning: Vocabulary=1, Grammar=2, Exercise=3, Conversation=4
            // Note: Conversation=4 in Learning maps to CurriculumItemType.Conversation=5
            var curriculumType = itemType switch
            {
                1 => CurriculumItemType.Vocabulary,
                2 => CurriculumItemType.Grammar,
                3 => CurriculumItemType.Exercise,
                4 => CurriculumItemType.Conversation,
                _ => (CurriculumItemType)itemType
            };

            var context = await curriculumQueryService.GetItemContextAsync(itemId, curriculumType, ct);
            if (context is null)
            {
                logger.LogWarning("AI evaluation skipped: item {ItemId} (type {ItemType}) not found in Curriculum.", itemId, itemType);
                return null;
            }

            var contextString = $"[{context.ItemType}] {context.Question}" +
                (context.Hint is not null ? $"\nHint: {context.Hint}" : "");

            var result = await geminiService.EvaluateInputAsync(contextString, userAnswer, ct);

            return new AiEvaluationResult(
                result.QualityScore,
                result.IsCorrect,
                result.SuggestedCorrection,
                result.Explanation);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI evaluation failed for ItemId={ItemId}. Caller should use fallback score.", itemId);
            return null;
        }
    }
}
