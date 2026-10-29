using System.Text.Json;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Application.Features.Vocabulary;
using EnglishLiveTutor.Domain.Grammar;
using EnglishLiveTutor.Domain.Grammar.Enums;
using EnglishLiveTutor.Domain.Lessons.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands;

public sealed record SubmitGrammarAnswerCommand(
    Guid AttemptId,
    Guid ExerciseId,
    JsonElement AnswerPayload,
    LessonAiExecutionConfigDto? AiConfig = null
) : ICommand<GrammarAnswerResultDto>;

public sealed class SubmitGrammarAnswerCommandHandler : ICommandHandler<SubmitGrammarAnswerCommand, GrammarAnswerResultDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IDeterministicExerciseGrader _deterministicGrader;
    private readonly ILessonAiGrader _aiGrader;
    private readonly IUnitOfWork _unitOfWork;

    public SubmitGrammarAnswerCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IDeterministicExerciseGrader deterministicGrader,
        ILessonAiGrader aiGrader,
        IUnitOfWork unitOfWork)
    {
        _db = db;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _deterministicGrader = deterministicGrader;
        _aiGrader = aiGrader;
        _unitOfWork = unitOfWork;
    }

    public async Task<GrammarAnswerResultDto> HandleAsync(SubmitGrammarAnswerCommand command, CancellationToken ct = default)
    {
        string? userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("Authentication required to submit grammar answer.");
        }

        var attempt = await _db.GrammarAttempts
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == command.AttemptId, ct);

        if (attempt == null || !string.Equals(attempt.UserId, userId, StringComparison.Ordinal))
        {
            throw new NotFoundException($"Grammar attempt with ID '{command.AttemptId}' was not found.");
        }

        if (attempt.CompletedAtUtc.HasValue)
        {
            throw new ConflictException("Cannot submit answer to a completed grammar attempt.");
        }

        if (!attempt.ExerciseIds.Contains(command.ExerciseId))
        {
            throw new ConflictException($"Exercise '{command.ExerciseId}' does not belong to attempt '{command.AttemptId}'.");
        }

        // Lock check for StepCore and LevelTest: each exercise only submitted once, unless existing is FailedGrading
        var existingAnswer = attempt.Answers.FirstOrDefault(a => a.ExerciseId == command.ExerciseId);
        if (existingAnswer != null && attempt.Kind is GrammarAttemptKind.StepCore or GrammarAttemptKind.LevelTest
            && !string.Equals(existingAnswer.Status, "FailedGrading", StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("grammar_answer_locked");
        }

        var exercise = await _db.GrammarExercises
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == command.ExerciseId, ct);

        if (exercise == null)
        {
            throw new NotFoundException($"Exercise '{command.ExerciseId}' was not found.");
        }

        string answerPayloadJson = command.AnswerPayload.GetRawText();
        if (answerPayloadJson.Length > 8000)
        {
            throw new ArgumentException("Answer payload must not exceed 8,000 characters.");
        }

        var now = _timeProvider.GetUtcNow();

        string status = "Completed";
        decimal? score100 = null;
        decimal? pointsEarned = null;
        string? feedbackJson = null;
        string? suggestedAnswer = null;

        if (exercise.GradingMode is GradingMode.Deterministic or GradingMode.Composite or GradingMode.SelfAssessment)
        {
            var gradeResult = _deterministicGrader.Grade(
                exercise.Type,
                exercise.ContentJson,
                exercise.GradingDefinitionJson,
                answerPayloadJson,
                exercise.MaxPoints);

            status = "Completed";
            score100 = gradeResult.Score100;
            pointsEarned = gradeResult.PointsEarned;
            feedbackJson = JsonSerializer.Serialize(new
            {
                isCorrect = gradeResult.IsCorrect,
                explanation = gradeResult.Explanation
            });
        }
        else if (exercise.GradingMode == GradingMode.AiRubric)
        {
            bool hasValidKey = !string.IsNullOrWhiteSpace(command.AiConfig?.ApiKey);

            if (hasValidKey)
            {
                string rubricCode = exercise.Type == ExerciseType.Translation
                    ? "LESSON_TRANSLATION"
                    : "LESSON_WRITING";
                int rubricVersion = 1;

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

                if (rubric == null)
                {
                    status = "FailedGrading";
                    suggestedAnswer = null;
                    feedbackJson = JsonSerializer.Serialize(new
                    {
                        error = "ai_grading_failed",
                        exceptionType = nameof(InvalidOperationException)
                    });
                }
                else
                {
                    // Call AI provider BEFORE any state changes or transaction
                    try
                    {
                        string? taskContextJson = BuildTaskContextJson(exercise);

                        var aiResult = await _aiGrader.GradeSubmissionAsync(
                            exercise.Type,
                            exercise.Instructions,
                            attempt.CefrLevel,
                            rubric,
                            answerPayloadJson,
                            exercise.MaxPoints,
                            taskContextJson: taskContextJson,
                            aiConfig: command.AiConfig,
                            ct: ct);

                        if (aiResult.IsAssessable)
                        {
                            status = "Completed";
                            score100 = aiResult.Score100;
                            pointsEarned = aiResult.PointsEarned;
                            suggestedAnswer = aiResult.SuggestedAnswer;

                            feedbackJson = JsonSerializer.Serialize(new
                            {
                                criteria = aiResult.CriteriaBreakdown,
                                strengths = aiResult.Strengths,
                                improvements = aiResult.Improvements,
                                suggestedAnswer = aiResult.SuggestedAnswer
                            });
                        }
                        else
                        {
                            status = "NotAssessable";
                            suggestedAnswer = null;
                            feedbackJson = JsonSerializer.Serialize(new { message = "Submission was not assessable." });
                        }
                    }
                    catch (Exception ex)
                    {
                        // Provider error -> FailedGrading, fixed code + exception type, no ex.Message
                        status = "FailedGrading";
                        suggestedAnswer = null;
                        feedbackJson = JsonSerializer.Serialize(new
                        {
                            error = "ai_grading_failed",
                            exceptionType = ex.GetType().Name
                        });
                    }
                }
            }
            else
            {
                // No BYO key -> SelfAssessed
                status = "SelfAssessed";
                score100 = null;
                pointsEarned = null;

                if (exercise.Type == ExerciseType.Translation)
                {
                    try
                    {
                        var gNode = JsonNode.Parse(exercise.GradingDefinitionJson ?? "{}");
                        var refArray = gNode?["referenceTranslations"]?.AsArray();
                        if (refArray != null && refArray.Count > 0)
                        {
                            suggestedAnswer = refArray[0]?.ToString();
                        }
                        else
                        {
                            suggestedAnswer = gNode?["referenceTranslation"]?.ToString();
                        }
                    }
                    catch { }
                }
                else
                {
                    // WritingPractice: read suggestedAnswer ONLY from GradingDefinitionJson, NEVER from ContentJson
                    try
                    {
                        var gNode = JsonNode.Parse(exercise.GradingDefinitionJson ?? "{}");
                        suggestedAnswer = gNode?["suggestedAnswer"]?.ToString();
                    }
                    catch { }
                }

                feedbackJson = JsonSerializer.Serialize(new
                {
                    status = "SelfAssessed",
                    suggestedAnswer = suggestedAnswer
                });
            }
        }
        else
        {
            throw new NotSupportedException($"Grading mode '{exercise.GradingMode}' is not supported.");
        }

        attempt.SubmitAnswer(
            exerciseId: exercise.Id,
            answerPayloadJson: answerPayloadJson,
            status: status,
            score100: score100,
            pointsEarned: pointsEarned,
            maxPoints: exercise.MaxPoints,
            feedbackJson: feedbackJson,
            contentHash: exercise.ContentHash,
            submittedAtUtc: now);

        try
        {
            await _unitOfWork.CommitChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("grammar_conflict");
        }
        catch (DbUpdateException ex) when (VocabularyGenerationWriter.IsUniqueConstraintViolation(ex))
        {
            throw new ConflictException("grammar_conflict");
        }

        JsonElement? feedbackElement = null;
        if (!string.IsNullOrWhiteSpace(feedbackJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(feedbackJson);
                feedbackElement = doc.RootElement.Clone();
            }
            catch { }
        }

        JsonElement? payloadElement = null;
        try
        {
            using var doc = JsonDocument.Parse(answerPayloadJson);
            payloadElement = doc.RootElement.Clone();
        }
        catch { }

        return new GrammarAnswerResultDto(
            AttemptId: attempt.Id,
            ExerciseId: exercise.Id,
            Status: status,
            Score100: score100,
            PointsEarned: pointsEarned,
            MaxPoints: exercise.MaxPoints,
            Feedback: feedbackElement,
            AnswerPayload: payloadElement,
            SuggestedAnswer: suggestedAnswer
        );
    }

    private static string? BuildTaskContextJson(GrammarExerciseEntity exercise)
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
            else if (exercise.Type == ExerciseType.WritingPractice)
            {
                if (contentNode?["prompt"] != null) contextObj["prompt"] = contentNode["prompt"]!.DeepClone();
                if (contentNode?["minWords"] != null) contextObj["minWords"] = contentNode["minWords"]!.DeepClone();
                if (contentNode?["maxWords"] != null) contextObj["maxWords"] = contentNode["maxWords"]!.DeepClone();
                if (gradingNode?["guidelines"] != null) contextObj["guidelines"] = gradingNode["guidelines"]!.DeepClone();
            }

            return contextObj.Count > 0 ? contextObj.ToJsonString() : null;
        }
        catch
        {
            return null;
        }
    }
}
