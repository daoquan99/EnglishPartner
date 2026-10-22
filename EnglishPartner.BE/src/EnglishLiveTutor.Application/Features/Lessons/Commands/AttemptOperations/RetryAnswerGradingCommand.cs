using System.Text.Json;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Common.Utilities;
using EnglishLiveTutor.Domain.LessonAttempts.Enums;
using EnglishLiveTutor.Domain.Lessons.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Lessons.Commands.AttemptOperations;

public sealed record RetryAnswerGradingCommand(
    Guid AttemptId,
    Guid AnswerId,
    int AttemptRevision,
    string? OperationKey = null,
    LessonAiExecutionConfigDto? AiConfig = null
) : ICommand<ExerciseAnswerResultDto>, IIdempotentCommand
{
    public string? IdempotencyKey => OperationKey;
}

public sealed class RetryAnswerGradingCommandHandler : ICommandHandler<RetryAnswerGradingCommand, ExerciseAnswerResultDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IDeterministicExerciseGrader _deterministicGrader;
    private readonly ILessonAiGrader _aiGrader;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyService _idempotency;

    public RetryAnswerGradingCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IDeterministicExerciseGrader deterministicGrader,
        ILessonAiGrader aiGrader,
        IUnitOfWork unitOfWork,
        IIdempotencyService idempotency)
    {
        _db = db;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _deterministicGrader = deterministicGrader;
        _aiGrader = aiGrader;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
    }

    public async Task<ExerciseAnswerResultDto> HandleAsync(RetryAnswerGradingCommand command, CancellationToken ct = default)
    {
        string? userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ForbiddenException("Authentication required to retry answer grading.");
        }

        string fingerprint = IdempotencyFingerprint.Compute(
            command,
            ("AttemptId", command.AttemptId),
            ("AnswerId", command.AnswerId),
            ("Revision", command.AttemptRevision));

        var replay = await _idempotency.CheckOrBeginOperationAsync(
            userId: userId,
            operationType: "LessonAttempt.RetryGrading",
            resourceScope: command.AttemptId.ToString(),
            operationKey: command.OperationKey ?? string.Empty,
            requestFingerprint: fingerprint,
            ct: ct);

        if (replay != null)
        {
            if (replay.StatusCode == 409)
            {
                throw new ConflictException(replay.ErrorMessage ?? "Conflict retrying answer grading.");
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
                await _idempotency.FailOperationAsync(userId, "LessonAttempt.RetryGrading", command.AttemptId.ToString(), command.OperationKey, CancellationToken.None);
            }
            throw new ForbiddenException("You are not authorized to retry grading for this attempt.");
        }

        var answer = attempt.Answers.FirstOrDefault(a => a.Id == command.AnswerId)
            ?? throw new NotFoundException($"Answer with ID '{command.AnswerId}' not found in attempt.");

        attempt.RetryGrading(command.AnswerId, command.AttemptRevision);

        var lesson = await _db.Lessons
            .Include(l => l.Exercises)
            .FirstOrDefaultAsync(l => l.Id == attempt.LessonId, ct)
            ?? throw new NotFoundException($"Lesson with ID '{attempt.LessonId}' not found.");

        var exercise = lesson.Exercises.FirstOrDefault(e => e.Id == answer.ExerciseId)
            ?? throw new NotFoundException($"Exercise with ID '{answer.ExerciseId}' not found.");

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
                answer.AnswerPayloadJson,
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
                        answer.AnswerPayloadJson,
                        exercise.MaxPoints,
                        taskContextJson,
                        command.AiConfig,
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
                    operationType: "LessonAttempt.RetryGrading",
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
                await _idempotency.FailOperationAsync(userId, "LessonAttempt.RetryGrading", command.AttemptId.ToString(), command.OperationKey, CancellationToken.None);
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
