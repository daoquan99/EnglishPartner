using EnglishPartner.Modules.AI.Contracts.Services;
using EnglishPartner.Modules.Curriculum.Contracts.Services;
using EnglishPartner.Modules.Learning.Domain.ItemMasteries;
using EnglishPartner.Modules.Learning.Domain.ItemMasteries.Repositories;
using EnglishPartner.Modules.Learning.Domain.StudySessions;
using EnglishPartner.Modules.Learning.Domain.StudySessions.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Learning.Application.StudySessions.Commands.SubmitAudioAnswer;

internal sealed class SubmitAudioAnswerCommandHandler(
    IStudySessionRepository sessionRepository,
    IItemMasteryRepository masteryRepository,
    ILearningActivityLogRepository logRepository,
    IAiEvaluationService aiEvaluationService,
    ICurriculumQueryService curriculumQueryService,
    TimeProvider timeProvider,
    ICurrentUserService currentUser) : ICommandHandler<SubmitAudioAnswerCommand, SubmitAudioAnswerResponse>
{
    private const int FallbackQualityScore = 0;

    public async Task<Result<SubmitAudioAnswerResponse>> Handle(SubmitAudioAnswerCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var session = await sessionRepository.GetByIdAsync(request.SessionId, cancellationToken);
        if (session is null || session.UserId != userId)
            return Result.Failure<SubmitAudioAnswerResponse>(CommonErrors.NotFound("StudySession", request.SessionId));

        session.Ping(utcNow);
        sessionRepository.Update(session);

        // Fetch curriculum context
        var curriculumType = (int)request.ItemType switch
        {
            0 => CurriculumItemType.Vocabulary,
            1 => CurriculumItemType.Grammar,
            2 => CurriculumItemType.ExampleSentence,
            _ => (CurriculumItemType)(int)request.ItemType
        };

        var context = await curriculumQueryService.GetItemContextAsync(request.ItemId, curriculumType, cancellationToken);
        if (context is null)
        {
            return Result.Failure<SubmitAudioAnswerResponse>(CommonErrors.NotFound("CurriculumItem", request.ItemId));
        }

        // Extract Target Text from context
        // Depending on ItemType, the Target Text is usually the CorrectAnswer (e.g. ExampleSentence translation text)
        // Or if it's vocabulary reading, it's the Question.
        // But for pronunciation practice, we need the exact word or sentence in the TargetLanguage.
        // We'll use CorrectAnswer as a fallback, but the user is usually reading the CorrectAnswer.
        // Since we didn't specify exactly, let's use the Question or CorrectAnswer based on the Type.
        // Usually, for speaking practice, the user reads the Target Language text.
        // GetVocabularyContextAsync sets CorrectAnswer to Definition (L1). The Question has the Word (L2).
        // Let's just use context.Question as the target text.
        var targetText = context.Question;

        using var stream = request.AudioFile.OpenReadStream();
        var evaluation = await aiEvaluationService.EvaluatePronunciationAsync(
            targetText,
            stream,
            context.LanguageCode,
            request.AudioFile.ContentType ?? "audio/webm",
            cancellationToken);

        var qualityScore = evaluation is not null 
            ? ScoreMapper.ToSm2QualityScore(evaluation.Score) 
            : FallbackQualityScore;

        var existingMastery = await masteryRepository.GetAsync(userId, request.ItemId, cancellationToken);
        var mastery = existingMastery ?? ItemMastery.Create(userId, request.ItemId, request.ItemType, utcNow);

        mastery.Review(qualityScore, utcNow);

        if (existingMastery is null) masteryRepository.Add(mastery);
        else masteryRepository.Update(mastery);

        var log = LearningActivityLog.Record(
            userId,
            request.SessionId,
            request.ItemId,
            userAnswer: "[AUDIO SUBMISSION]",
            isCorrect: qualityScore >= 3,
            scoreGranted: qualityScore,
            request.TimeSpentSeconds,
            utcNow,
            rawScore: evaluation?.Score,
            aiFeedback: evaluation?.PronunciationFeedback);

        logRepository.Add(log);

        return new SubmitAudioAnswerResponse(
            qualityScore,
            evaluation?.Transcription ?? "",
            evaluation?.PronunciationFeedback,
            mastery.IntervalDays,
            mastery.EasinessFactor,
            mastery.NextReviewDate);
    }
}
