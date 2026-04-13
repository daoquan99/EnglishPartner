using EnglishPartner.Modules.Learning.Domain.ItemMasteries;
using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Learning.Application.StudySessions.Commands.SubmitAnswer;

/// <summary>
/// Records a user's answer and applies the SM-2 algorithm to update the item's mastery schedule.
/// For simple items (Vocabulary, Grammar): client sends QualityScore directly.
/// For complex items (Conversation, ExampleSentence, Exercise): client first calls AI /evaluate, 
/// then passes the returned QualityScore here.
/// </summary>
public sealed record SubmitAnswerCommand(
    Guid UserId,
    Guid SessionId,
    Guid ItemId,
    ItemType ItemType,
    string UserAnswer,
    int QualityScore,
    int TimeSpentSeconds) : ICommand<SubmitAnswerResponse>;

public sealed record SubmitAnswerResponse(
    int QualityScore,
    bool IsCorrect,
    int NewIntervalDays,
    double NewEasinessFactor,
    DateTime NextReviewDate);
