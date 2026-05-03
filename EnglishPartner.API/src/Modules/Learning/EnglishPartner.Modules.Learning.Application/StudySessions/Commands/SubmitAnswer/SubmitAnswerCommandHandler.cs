using EnglishPartner.Modules.AI.Contracts.Services;
using EnglishPartner.Modules.Learning.Domain.ItemMasteries;
using EnglishPartner.Modules.Learning.Domain.ItemMasteries.Repositories;
using EnglishPartner.Modules.Learning.Domain.StudySessions;
using EnglishPartner.Modules.Learning.Domain.StudySessions.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Learning.Application.StudySessions.Commands.SubmitAnswer;

internal sealed class SubmitAnswerCommandHandler(
    IStudySessionRepository sessionRepository,
    IItemMasteryRepository masteryRepository,
    ILearningActivityLogRepository logRepository,
    IAiEvaluationService aiEvaluationService,
    EnglishPartner.Modules.Curriculum.Contracts.Services.ICurriculumQueryService curriculumQueryService,
    TimeProvider timeProvider,
    ICurrentUserService currentUser) : ICommandHandler<SubmitAnswerCommand, SubmitAnswerResponse>
{
    // Fallback score when AI evaluation is unavailable (degraded mode — SM-2 still runs)
    private const int FallbackQualityScore = 0;

    public async Task<Result<SubmitAnswerResponse>> Handle(SubmitAnswerCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var session = await sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
        if (session is null || session.UserId != userId)
            return Result.Failure<SubmitAnswerResponse>(CommonErrors.NotFound("StudySession", request.SessionId));

        session.Ping(utcNow);
        sessionRepository.Update(session);

        // ── Fetch Curriculum Context ──
        // Map Learning.ItemType -> CurriculumItemType
        var curriculumType = (int)request.ItemType switch
        {
            0 => Curriculum.Contracts.Services.CurriculumItemType.Vocabulary,
            1 => Curriculum.Contracts.Services.CurriculumItemType.Grammar,
            2 => Curriculum.Contracts.Services.CurriculumItemType.ExampleSentence,
            3 => Curriculum.Contracts.Services.CurriculumItemType.Topic,
            4 => Curriculum.Contracts.Services.CurriculumItemType.Conversation,
            5 => Curriculum.Contracts.Services.CurriculumItemType.Exercise,
            _ => (Curriculum.Contracts.Services.CurriculumItemType)(int)request.ItemType
        };

        var context = await curriculumQueryService.GetItemContextAsync(request.ItemId, curriculumType, cancellationToken);
        
        // ── Scoring ──
        AiTextEvaluationResult? evaluation = null;
        int qualityScore;

        if (request.ItemType == ItemType.Conversation && int.TryParse(request.UserAnswer, out var conversationAvgScore))
        {
            // Conversation role-play: FE sends the average pronunciation score (0-100)
            // aggregated from evaluate-line calls. No need to call AI again.
            var clampedScore = Math.Clamp(conversationAvgScore, 0, 100);
            qualityScore = ScoreMapper.ToSm2QualityScore(clampedScore);
            evaluation = new AiTextEvaluationResult(clampedScore, qualityScore >= 3, null, null);
        }
        else if (context is not null)
        {
            // Standard flow: call AI to evaluate text answer
            var question = $"[{context.ItemType}] {context.Question}" +
                           (context.Hint is not null ? $"\nHint: {context.Hint}" : "");
            evaluation = await aiEvaluationService.EvaluateTextAnswerAsync(
                question, context.CorrectAnswer, request.UserAnswer, cancellationToken);
            qualityScore = evaluation is not null
                ? ScoreMapper.ToSm2QualityScore(evaluation.Score)
                : FallbackQualityScore;
        }
        else
        {
            qualityScore = FallbackQualityScore;
        }

        // ── SM-2: Find or create ItemMastery ──
        var existingMastery = await masteryRepository.GetAsync(userId, request.ItemId, cancellationToken);
        var mastery = existingMastery ?? ItemMastery.Create(userId, request.ItemId, request.ItemType, utcNow);

        mastery.Review(qualityScore, utcNow);

        if (existingMastery is null) masteryRepository.Add(mastery);
        else masteryRepository.Update(mastery);

        // ── Log the raw activity ──
        var log = LearningActivityLog.Record(
            userId,
            request.SessionId,
            request.ItemId,
            request.UserAnswer,
            isCorrect: evaluation?.IsCorrect ?? false,
            scoreGranted: qualityScore,
            request.TimeSpentSeconds,
            utcNow,
            rawScore: evaluation?.Score,
            aiFeedback: evaluation?.Explanation ?? evaluation?.SuggestedCorrection);

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
