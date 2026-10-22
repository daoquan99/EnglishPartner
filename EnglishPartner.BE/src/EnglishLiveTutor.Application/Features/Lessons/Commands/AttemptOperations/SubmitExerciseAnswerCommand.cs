using System.Text.Json;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Common.Utilities;
using EnglishLiveTutor.Domain.LessonAttempts.Enums;
using EnglishLiveTutor.Domain.Lessons.Enums;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Lessons.Commands.AttemptOperations;

public sealed record SubmitExerciseAnswerCommand(
    Guid AttemptId,
    SubmitExerciseAnswerRequest Request,
    string? OperationKey = null
) : ICommand<ExerciseAnswerResultDto>, IIdempotentCommand
{
    public string? IdempotencyKey => OperationKey;
}

public sealed class SubmitExerciseAnswerCommandHandler : ICommandHandler<SubmitExerciseAnswerCommand, ExerciseAnswerResultDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IDeterministicExerciseGrader _deterministicGrader;
    private readonly ILessonAiGrader _aiGrader;
    private readonly IAudioRecordingRepository _audioRepo;
    private readonly ILessonMediaStorage _mediaStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyService _idempotency;

    public SubmitExerciseAnswerCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IDeterministicExerciseGrader deterministicGrader,
        ILessonAiGrader aiGrader,
        IAudioRecordingRepository audioRepo,
        ILessonMediaStorage mediaStorage,
        IUnitOfWork unitOfWork,
        IIdempotencyService idempotency)
    {
        _db = db;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _deterministicGrader = deterministicGrader;
        _aiGrader = aiGrader;
        _audioRepo = audioRepo;
        _mediaStorage = mediaStorage;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
    }

    public async Task<ExerciseAnswerResultDto> HandleAsync(SubmitExerciseAnswerCommand command, CancellationToken ct = default)
    {
        string? userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ForbiddenException("Authentication required to submit exercise answer.");
        }

        // 1. Check idempotency BEFORE checking revision
        string fingerprint = IdempotencyFingerprint.Compute(
            command.Request,
            ("AttemptId", command.AttemptId),
            ("ExerciseId", command.Request.ExerciseId));

        var replay = await _idempotency.CheckOrBeginOperationAsync(
            userId: userId,
            operationType: "LessonAttempt.SubmitAnswer",
            resourceScope: command.AttemptId.ToString(),
            operationKey: command.OperationKey ?? string.Empty,
            requestFingerprint: fingerprint,
            ct: ct);

        if (replay != null)
        {
            if (replay.StatusCode == 409)
            {
                throw new ConflictException(replay.ErrorMessage ?? "Conflict submitting exercise answer.");
            }

            var cached = JsonSerializer.Deserialize<ExerciseAnswerResultDto>(replay.ResponsePayloadJson!);
            return cached!;
        }

        var attempt = await _db.LessonAttempts
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == command.AttemptId, ct)
            ?? throw new NotFoundException($"Attempt with ID '{command.AttemptId}' not found.");

        if (!string.Equals(attempt.UserId, userId, StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(command.OperationKey))
            {
                await _idempotency.FailOperationAsync(userId, "LessonAttempt.SubmitAnswer", command.AttemptId.ToString(), command.OperationKey, CancellationToken.None);
            }
            throw new ForbiddenException("You are not authorized to submit answers for this attempt.");
        }

        var req = command.Request;

        // Fetch lesson exercise to get grading configuration
        var lesson = await _db.Lessons
            .Include(l => l.Exercises)
            .FirstOrDefaultAsync(l => l.Id == attempt.LessonId, ct)
            ?? throw new NotFoundException($"Lesson with ID '{attempt.LessonId}' not found.");

        var exercise = lesson.Exercises.FirstOrDefault(e => e.Id == req.ExerciseId)
            ?? throw new NotFoundException($"Exercise with ID '{req.ExerciseId}' not found in lesson.");

        string answerPayloadJson = req.AnswerPayload?.ToJsonString() ?? "{}";

        // Validate audio asset if present
        if (req.AnswerPayload != null && req.AnswerPayload["audioAssetId"] != null)
        {
            string audioIdStr = req.AnswerPayload["audioAssetId"]!.ToString();
            if (Guid.TryParse(audioIdStr, out var audioId))
            {
                var audioRec = await _audioRepo.GetByIdAsync(audioId, ct);
                if (audioRec == null || audioRec.IsDeleted || !string.Equals(audioRec.UserId, userId, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Audio asset does not exist or does not belong to the current user.");
                }

                bool bytesExist = await _mediaStorage.ExistsAsync(audioRec.StorageKey, ct);
                if (!bytesExist)
                {
                    throw new InvalidOperationException("Audio recording has no stored audio data.");
                }
            }
        }

        // Record submission (increments AttemptRevision)
        var answer = attempt.SubmitAnswer(
            exerciseId: req.ExerciseId,
            subQuestionId: req.SubQuestionId,
            expectedRevision: req.AttemptRevision,
            gradingMode: exercise.GradingMode,
            maxPoints: exercise.MaxPoints,
            answerPayloadJson: answerPayloadJson,
            initialStatus: SubmissionGradingStatus.Pending);

        decimal? score100 = null;
        decimal? pointsEarned = null;
        string? rubricCode = null;
        int? rubricVersion = null;
        List<CriterionScoreBreakdownDto>? breakdown = null;
        List<string>? strengths = null;
        List<string>? improvements = null;
        string? suggestedAnswer = null;

        var now = _timeProvider.GetUtcNow();

        if (exercise.GradingMode is GradingMode.SelfAssessment or GradingMode.Deterministic or GradingMode.Composite)
        {
            var result = _deterministicGrader.Grade(
                exercise.Type,
                exercise.ContentJson,
                exercise.GradingDefinitionJson,
                answerPayloadJson,
                exercise.MaxPoints);

            score100 = result.Score100;
            pointsEarned = result.PointsEarned;

            attempt.ApplyGradingResult(
                answer.Id,
                SubmissionGradingStatus.Completed,
                score100: score100,
                pointsEarned: pointsEarned,
                gradingResultJson: result.Explanation,
                gradedAtUtc: now);
        }
        else if (exercise.GradingMode == GradingMode.AiRubric)
        {
            rubricCode = "LESSON_WRITING";
            rubricVersion = 1;

            if (!string.IsNullOrWhiteSpace(exercise.GradingDefinitionJson))
            {
                try
                {
                    var gNode = JsonNode.Parse(exercise.GradingDefinitionJson);
                    rubricCode = gNode?["rubricCode"]?.ToString() ?? rubricCode;
                    rubricVersion = gNode?["rubricVersion"]?.GetValue<int>() ?? 1;
                }
                catch { }
            }

            var rubric = await _db.ScoringRubrics
                .Include(r => r.Criteria)
                .FirstOrDefaultAsync(r => r.Code == rubricCode && r.Version == rubricVersion, ct);

            if (rubric != null)
            {
                try
                {
                    var aiResult = await _aiGrader.GradeSubmissionAsync(
                        exercise.Type,
                        exercise.Instructions,
                        lesson.CefrLevel,
                        rubric,
                        answerPayloadJson,
                        exercise.MaxPoints,
                        ct);

                    if (aiResult.IsAssessable)
                    {
                        score100 = aiResult.Score100;
                        pointsEarned = aiResult.PointsEarned;
                        breakdown = aiResult.CriteriaBreakdown.Select(c => new CriterionScoreBreakdownDto(
                            CriterionCode: c.CriterionCode,
                            Name: c.Name,
                            Weight: c.Weight,
                            RawScore: c.RawScore,
                            Descriptor: c.Descriptor,
                            Feedback: c.Feedback
                        )).ToList();

                        strengths = aiResult.Strengths;
                        improvements = aiResult.Improvements;
                        suggestedAnswer = aiResult.SuggestedAnswer;

                        attempt.ApplyGradingResult(
                            answer.Id,
                            SubmissionGradingStatus.Completed,
                            score100: score100,
                            pointsEarned: pointsEarned,
                            gradingResultJson: JsonSerializer.Serialize(breakdown),
                            gradedAtUtc: now);
                    }
                    else
                    {
                        attempt.ApplyGradingResult(
                            answer.Id,
                            SubmissionGradingStatus.NotAssessable,
                            score100: null,
                            pointsEarned: null,
                            gradingResultJson: "Submission was not assessable.",
                            gradedAtUtc: now);
                    }
                }
                catch (Exception ex)
                {
                    attempt.ApplyGradingResult(
                        answer.Id,
                        SubmissionGradingStatus.Failed,
                        score100: null,
                        pointsEarned: null,
                        gradingResultJson: $"AI grading error: {ex.Message}",
                        gradedAtUtc: now);
                }
            }
            else
            {
                attempt.ApplyGradingResult(
                    answer.Id,
                    SubmissionGradingStatus.Failed,
                    score100: null,
                    pointsEarned: null,
                    gradingResultJson: $"Rubric '{rubricCode}' version {rubricVersion} not found.",
                    gradedAtUtc: now);
            }
        }

        try
        {
            await _unitOfWork.CommitChangesAsync(ct);

            var progress = AttemptProgressCalculator.CalculateProgress(attempt, lesson.Exercises);

            var resultDto = new ExerciseAnswerResultDto(
                AnswerId: answer.Id,
                AttemptId: attempt.Id,
                AttemptRevision: attempt.AttemptRevision,
                ExerciseId: answer.ExerciseId,
                SubQuestionId: answer.SubQuestionId,
                Status: answer.Status.ToString(),
                GradingMode: answer.GradingMode.ToString(),
                Score100: answer.Score100,
                PointsEarned: answer.PointsEarned,
                MaxPoints: answer.MaxPoints,
                RubricCode: rubricCode,
                RubricVersion: rubricVersion,
                CriteriaBreakdown: breakdown,
                Strengths: strengths,
                Improvements: improvements,
                SuggestedAnswer: suggestedAnswer,
                AttemptProgress: progress);

            if (!string.IsNullOrWhiteSpace(command.OperationKey))
            {
                await _idempotency.CompleteOperationAsync(
                    userId: userId,
                    operationType: "LessonAttempt.SubmitAnswer",
                    resourceScope: command.AttemptId.ToString(),
                    operationKey: command.OperationKey,
                    statusCode: 200,
                    responsePayload: resultDto,
                    ct: ct);
            }

            return resultDto;
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(command.OperationKey))
            {
                await _idempotency.FailOperationAsync(userId, "LessonAttempt.SubmitAnswer", command.AttemptId.ToString(), command.OperationKey, CancellationToken.None);
            }
            throw;
        }
    }
}
