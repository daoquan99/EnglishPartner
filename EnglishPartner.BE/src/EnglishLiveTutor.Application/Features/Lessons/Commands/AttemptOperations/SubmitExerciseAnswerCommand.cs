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

        var lesson = await _db.Lessons
            .Include(l => l.Exercises)
            .FirstOrDefaultAsync(l => l.Id == attempt.LessonId, ct)
            ?? throw new NotFoundException($"Lesson with ID '{attempt.LessonId}' not found.");

        var exercise = lesson.Exercises.FirstOrDefault(e => e.Id == req.ExerciseId)
            ?? throw new NotFoundException($"Exercise with ID '{req.ExerciseId}' not found in lesson.");

        string answerPayloadJson = req.AnswerPayload?.ToJsonString() ?? "{}";

        // Validate audio asset if present or required
        if (req.AnswerPayload != null && req.AnswerPayload["audioAssetId"] != null)
        {
            string audioIdStr = req.AnswerPayload["audioAssetId"]!.ToString();
            if (!Guid.TryParse(audioIdStr, out var audioId))
            {
                throw new ArgumentException("Audio asset ID must be a valid GUID.");
            }

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
        else if (exercise.Type == ExerciseType.Shadowing)
        {
            throw new ArgumentException("Shadowing exercise requires a valid recorded audio submission.");
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

        if (exercise.GradingMode is GradingMode.SelfAssessment or GradingMode.Deterministic)
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
        else if (exercise.GradingMode == GradingMode.Composite)
        {
            var contentObj = JsonNode.Parse(exercise.ContentJson);
            var subQArray = contentObj?["subQuestions"]?.AsArray();
            bool hasShortAnswer = subQArray?.Any(q => string.Equals(q?["questionKind"]?.ToString(), "ShortAnswer", StringComparison.OrdinalIgnoreCase)) ?? false;

            if (!hasShortAnswer)
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
            else
            {
                // Composite with ShortAnswer: closed questions graded deterministically, ShortAnswer through AI
                try
                {
                    decimal totalEarned = 0m;
                    decimal totalMax = 0m;
                    var submittedArray = JsonNode.Parse(answerPayloadJson)?["subQuestionAnswers"]?.AsArray();

                    var shortAnswerRubric = await _db.ScoringRubrics
                        .Include(r => r.Criteria)
                        .FirstOrDefaultAsync(r => r.Code == "LESSON_SHORT_ANSWER" && r.Version == 1, ct);

                    if (shortAnswerRubric == null)
                    {
                        throw new InvalidOperationException("Scoring rubric 'LESSON_SHORT_ANSWER' v1 not found.");
                    }

                    foreach (var q in subQArray!)
                    {
                        string qId = q?["id"]?.ToString() ?? "";
                        string qKind = q?["questionKind"]?.ToString() ?? "ShortAnswer";
                        decimal qMax = q?["maxPoints"]?.GetValue<decimal>() ?? 5m;
                        totalMax += qMax;

                        var subSubmission = submittedArray?.FirstOrDefault(s => s?["questionId"]?.ToString() == qId);

                        if (qKind == "ShortAnswer")
                        {
                            string learnerText = subSubmission?["textAnswer"]?.ToString() ?? "";
                            string qText = q?["questionText"]?.ToString() ?? "";
                            string refText = q?["grading"]?["referenceAnswer"]?.ToString() ?? q?["grading"]?["referenceAnswers"]?.ToString() ?? "";

                            string taskCtx = JsonSerializer.Serialize(new { questionText = qText, referenceAnswer = refText });

                            var aiRes = await _aiGrader.GradeSubmissionAsync(
                                exercise.Type,
                                qText,
                                lesson.CefrLevel,
                                shortAnswerRubric,
                                JsonSerializer.Serialize(new { writtenText = learnerText }),
                                qMax,
                                taskCtx,
                                req.AiConfig,
                                ct);

                            if (aiRes.IsAssessable)
                            {
                                totalEarned += aiRes.PointsEarned;
                            }
                        }
                        else
                        {
                            // Closed question
                            var singleQContent = new JsonObject
                            {
                                ["subQuestions"] = new JsonArray { q!.DeepClone() }
                            };
                            var singleQAnswer = new JsonObject
                            {
                                ["subQuestionAnswers"] = new JsonArray { subSubmission?.DeepClone() ?? new JsonObject() }
                            };
                            var subRes = _deterministicGrader.Grade(exercise.Type, singleQContent.ToJsonString(), "{}", singleQAnswer.ToJsonString(), qMax);
                            totalEarned += subRes.PointsEarned;
                        }
                    }

                    score100 = totalMax > 0 ? Math.Round((totalEarned / totalMax) * 100m, 2, MidpointRounding.AwayFromZero) : 100m;
                    pointsEarned = Math.Round((score100.Value / 100m) * exercise.MaxPoints, 2, MidpointRounding.AwayFromZero);

                    attempt.ApplyGradingResult(
                        answer.Id,
                        SubmissionGradingStatus.Completed,
                        score100: score100,
                        pointsEarned: pointsEarned,
                        gradingResultJson: $"Đạt {totalEarned}/{totalMax} điểm câu hỏi composite.",
                        gradedAtUtc: now);
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
        }
        else if (exercise.GradingMode == GradingMode.AiRubric)
        {
            rubricCode = exercise.Type switch
            {
                ExerciseType.Translation => "LESSON_TRANSLATION",
                ExerciseType.SpeakingPractice => "LESSON_SPEAKING",
                ExerciseType.Shadowing => "LESSON_SHADOWING",
                ExerciseType.ConversationRolePlay => "LESSON_CONVERSATION_TEXT",
                ExerciseType.WritingPractice => "LESSON_WRITING",
                _ => "LESSON_WRITING"
            };
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

            if (rubricCode is "LESSON_ROLEPLAY" or "LESSON_CONVERSATION")
            {
                rubricCode = "LESSON_CONVERSATION_TEXT";
            }

            var rubric = await _db.ScoringRubrics
                .Include(r => r.Criteria)
                .FirstOrDefaultAsync(r => r.Code == rubricCode && r.Version == rubricVersion, ct);

            if (rubric != null)
            {
                try
                {
                    string? taskContextJson = BuildTaskContextJson(exercise);

                    var aiResult = await _aiGrader.GradeSubmissionAsync(
                        exercise.Type,
                        exercise.Instructions,
                        lesson.CefrLevel,
                        rubric,
                        answerPayloadJson,
                        exercise.MaxPoints,
                        taskContextJson,
                        req.AiConfig,
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

            string? errorMessage = answer.Status is SubmissionGradingStatus.Failed or SubmissionGradingStatus.NotAssessable
                ? answer.GradingResultJson
                : null;

            JsonNode? answerPayload = null;
            if (!string.IsNullOrWhiteSpace(answer.AnswerPayloadJson))
            {
                try { answerPayload = JsonNode.Parse(answer.AnswerPayloadJson); } catch { }
            }

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
                AttemptProgress: progress,
                ErrorMessage: errorMessage,
                AnswerPayload: answerPayload);

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

    private static string? BuildTaskContextJson(EnglishLiveTutor.Domain.Lessons.Entities.ExerciseEntity exercise)
    {
        var contextObj = new JsonObject();
        try
        {
            var contentNode = JsonNode.Parse(exercise.ContentJson);
            var gradingNode = JsonNode.Parse(exercise.GradingDefinitionJson ?? "{}");

            if (exercise.Type == ExerciseType.Translation)
            {
                if (contentNode?["sourceText"] != null) contextObj["sourceText"] = contentNode["sourceText"]!.DeepClone();
                if (contentNode?["direction"] != null) contextObj["direction"] = contentNode["direction"]!.DeepClone();
                if (contentNode?["context"] != null) contextObj["context"] = contentNode["context"]!.DeepClone();
                if (gradingNode?["referenceTranslations"] != null) contextObj["referenceTranslations"] = gradingNode["referenceTranslations"]!.DeepClone();
                if (gradingNode?["requiredKeyIdeas"] != null) contextObj["requiredKeyIdeas"] = gradingNode["requiredKeyIdeas"]!.DeepClone();
            }
            else if (exercise.Type == ExerciseType.Shadowing)
            {
                if (contentNode?["referenceText"] != null) contextObj["referenceText"] = contentNode["referenceText"]!.DeepClone();
                else if (contentNode?["transcript"] != null) contextObj["referenceText"] = contentNode["transcript"]!.DeepClone();
                if (contentNode?["speechRateWpm"] != null) contextObj["speechRateWpm"] = contentNode["speechRateWpm"]!.DeepClone();
            }
            else if (exercise.Type == ExerciseType.WritingPractice)
            {
                if (contentNode?["prompt"] != null) contextObj["prompt"] = contentNode["prompt"]!.DeepClone();
                if (contentNode?["minWords"] != null) contextObj["minWords"] = contentNode["minWords"]!.DeepClone();
                if (contentNode?["maxWords"] != null) contextObj["maxWords"] = contentNode["maxWords"]!.DeepClone();
                if (gradingNode?["guidelines"] != null) contextObj["guidelines"] = gradingNode["guidelines"]!.DeepClone();
            }
            else if (exercise.Type == ExerciseType.SpeakingPractice)
            {
                if (contentNode?["topic"] != null) contextObj["topic"] = contentNode["topic"]!.DeepClone();
                if (contentNode?["instructions"] != null) contextObj["instructions"] = contentNode["instructions"]!.DeepClone();
                if (contentNode?["targetDurationSeconds"] != null) contextObj["targetDurationSeconds"] = contentNode["targetDurationSeconds"]!.DeepClone();
                if (contentNode?["suggestedPrompts"] != null) contextObj["suggestedPrompts"] = contentNode["suggestedPrompts"]!.DeepClone();
            }
            return contextObj.Count > 0 ? contextObj.ToJsonString() : null;
        }
        catch
        {
            return null;
        }
    }
}
