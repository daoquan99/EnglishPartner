using EnglishPartner.Modules.AI.Contracts.Services;
using EnglishPartner.Modules.Learning.Domain.ItemMasteries;
using EnglishPartner.Modules.Learning.Domain.ItemMasteries.Repositories;
using EnglishPartner.Modules.Learning.Domain.StudySessions;
using EnglishPartner.Modules.Learning.Domain.StudySessions.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Learning.Application.StudySessions.Commands.SubmitAnswer;

internal sealed class SubmitAnswerCommandHandler(
    IStudySessionRepository sessionRepository,
    IItemMasteryRepository masteryRepository,
    ILearningActivityLogRepository logRepository,
    IAiEvaluationService aiEvaluationService) : ICommandHandler<SubmitAnswerCommand, SubmitAnswerResponse>
{
    // Fallback score when AI evaluation is unavailable (degraded mode — SM-2 still runs)
    private const int FallbackQualityScore = 0;

    public async Task<Result<SubmitAnswerResponse>> Handle(SubmitAnswerCommand request, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
        if (session is null || session.UserId != request.UserId)
            return Result.Failure<SubmitAnswerResponse>(CommonErrors.NotFound("StudySession", request.SessionId));

        session.Ping(DateTime.UtcNow);
        sessionRepository.Update(session);

        // ── AI Evaluation (server-side scoring) ──
        var evaluation = await aiEvaluationService.EvaluateAsync(
            request.ItemId, (int)request.ItemType, request.UserAnswer, cancellationToken);

        var qualityScore = evaluation?.QualityScore ?? FallbackQualityScore;

        // ── SM-2: Find or create ItemMastery ──
        var existingMastery = await masteryRepository.GetAsync(request.UserId, request.ItemId, cancellationToken);
        var mastery = existingMastery ?? ItemMastery.Create(request.UserId, request.ItemId, request.ItemType);

        mastery.Review(qualityScore, DateTime.UtcNow);

        if (existingMastery is null) masteryRepository.Add(mastery);
        else masteryRepository.Update(mastery);

        // ── Log the raw activity ──
        var log = LearningActivityLog.Record(
            request.UserId,
            request.SessionId,
            request.ItemId,
            request.UserAnswer,
            isCorrect: evaluation?.IsCorrect ?? false,
            scoreGranted: qualityScore,
            request.TimeSpentSeconds);

        logRepository.Add(log);

        return new SubmitAnswerResponse(
            qualityScore,
            evaluation?.IsCorrect ?? false,
            evaluation?.SuggestedCorrection,
            evaluation?.Explanation,
            mastery.IntervalDays,
            mastery.EasinessFactor,
            mastery.NextReviewDate);
    }
}
