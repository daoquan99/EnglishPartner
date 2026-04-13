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
    ILearningActivityLogRepository logRepository) : ICommandHandler<SubmitAnswerCommand, SubmitAnswerResponse>
{
    public async Task<Result<SubmitAnswerResponse>> Handle(SubmitAnswerCommand request, CancellationToken cancellationToken)
    {
        var session = await sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
        if (session is null || session.UserId != request.UserId)
            return Result.Failure<SubmitAnswerResponse>(new Error("StudySession.NotFound", "Session not found."));

        session.Ping(DateTime.UtcNow);
        sessionRepository.Update(session);

        // ── SM-2: Find or create ItemMastery ──
        var existingMastery = await masteryRepository.GetAsync(request.UserId, request.ItemId, cancellationToken);
        var mastery = existingMastery ?? ItemMastery.Create(request.UserId, request.ItemId, request.ItemType);

        mastery.Review(request.QualityScore, DateTime.UtcNow);

        if (existingMastery is null) masteryRepository.Add(mastery);
        else masteryRepository.Update(mastery);

        // ── Log the raw activity ──
        var log = LearningActivityLog.Record(
            request.UserId,
            request.SessionId,
            request.ItemId,
            request.UserAnswer,
            isCorrect: request.QualityScore >= 3,
            scoreGranted: request.QualityScore,
            request.TimeSpentSeconds);

        logRepository.Add(log);

        return new SubmitAnswerResponse(
            request.QualityScore,
            request.QualityScore >= 3,
            mastery.IntervalDays,
            mastery.EasinessFactor,
            mastery.NextReviewDate);
    }
}
