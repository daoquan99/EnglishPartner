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
            // Map Learning.ItemType (0-based) → CurriculumItemType (1-based, different order)
            // Learning:   Vocabulary=0, Grammar=1, ExampleSentence=2, Topic=3, Conversation=4, Exercise=5
            // Curriculum: Vocabulary=1, Grammar=2, Exercise=3, ExampleSentence=4, Conversation=5, Topic=6
            var curriculumType = itemType switch
            {
                0 => CurriculumItemType.Vocabulary,
                1 => CurriculumItemType.Grammar,
                2 => CurriculumItemType.ExampleSentence,
                3 => CurriculumItemType.Topic,
                4 => CurriculumItemType.Conversation,
                5 => CurriculumItemType.Exercise,
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
