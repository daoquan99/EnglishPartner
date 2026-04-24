using EnglishPartner.Modules.Learning.Domain.ItemMasteries;
using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Learning.Application.StudySessions.Commands.SubmitAnswer;

/// <summary>
/// Records a user's answer in a study session and applies the SM-2 algorithm.
/// 
/// QualityScore is now computed SERVER-SIDE via the AI evaluation service.
/// Client sends the raw answer — AI grades it internally.
/// Fallback score of 0 is used if AI evaluation is unavailable (degraded mode).
/// </summary>
public sealed record SubmitAnswerCommand(
    Guid SessionId,
    Guid ItemId,
    ItemType ItemType,
    string UserAnswer,
    int TimeSpentSeconds) : ICommand<SubmitAnswerResponse>;

public sealed record SubmitAnswerResponse(
    int QualityScore,
    bool IsCorrect,
    string? SuggestedCorrection,
    string? Explanation,
    int NewIntervalDays,
    double NewEasinessFactor,
    DateTime NextReviewDate);
