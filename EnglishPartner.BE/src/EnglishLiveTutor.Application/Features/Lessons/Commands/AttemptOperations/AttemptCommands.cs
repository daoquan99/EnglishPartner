using System.Text.Json;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Lessons.Queries.AttemptQueries;
using EnglishLiveTutor.Domain.LessonAttempts;
using EnglishLiveTutor.Domain.LessonAttempts.Enums;
using EnglishLiveTutor.Domain.Lessons.Entities;
using EnglishLiveTutor.Domain.Lessons.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Lessons.Commands.AttemptOperations;

public sealed record StartLessonAttemptCommand(Guid LessonId) : ICommand<LessonAttemptResumeDto>;

public sealed record SubmitExerciseAnswerCommand(
    Guid AttemptId,
    SubmitExerciseAnswerRequest Request,
    string? OperationKey = null
) : ICommand<ExerciseAnswerResultDto>;

public sealed record RetryAnswerGradingCommand(
    Guid AttemptId,
    Guid AnswerId,
    int AttemptRevision
) : ICommand<ExerciseAnswerResultDto>;

public sealed record CompleteLessonAttemptCommand(Guid AttemptId) : ICommand<LessonAttemptResumeDto>;

public sealed class AttemptCommandHandlers :
    ICommandHandler<StartLessonAttemptCommand, LessonAttemptResumeDto>,
    ICommandHandler<SubmitExerciseAnswerCommand, ExerciseAnswerResultDto>,
    ICommandHandler<RetryAnswerGradingCommand, ExerciseAnswerResultDto>,
    ICommandHandler<CompleteLessonAttemptCommand, LessonAttemptResumeDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IDeterministicExerciseGrader _deterministicGrader;
    private readonly ILessonAiGrader _aiGrader;
    private readonly IQueryHandler<ResumeLessonAttemptQuery, LessonAttemptResumeDto?> _resumeHandler;

    public AttemptCommandHandlers(
        IAppDbContext db,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IDeterministicExerciseGrader deterministicGrader,
        ILessonAiGrader aiGrader,
        IQueryHandler<ResumeLessonAttemptQuery, LessonAttemptResumeDto?> resumeHandler)
    {
        _db = db;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _deterministicGrader = deterministicGrader;
        _aiGrader = aiGrader;
        _resumeHandler = resumeHandler;
    }

    public async Task<LessonAttemptResumeDto> HandleAsync(StartLessonAttemptCommand command, CancellationToken ct = default)
    {
        string userId = _currentUser.UserId ?? "anonymous";

        var lesson = await _db.Lessons
            .Include(l => l.Exercises)
            .FirstOrDefaultAsync(l => l.Id == command.LessonId, ct)
            ?? throw new NotFoundException($"Lesson with ID '{command.LessonId}' not found.");

        if (lesson.Status != LessonStatus.Published)
            throw new InvalidOperationException("Cannot start an attempt for an un-published lesson.");

        decimal totalMaxPoints = lesson.Exercises
            .Where(e => e.GradingMode != GradingMode.SelfAssessment)
            .Sum(e => e.MaxPoints);

        var attempt = LessonAttemptEntity.Start(
            lessonId: lesson.Id,
            userId: userId,
            pinnedLessonVersion: lesson.Version,
            totalMaxPoints: totalMaxPoints);

        _db.LessonAttempts.Add(attempt);
        await _db.SaveChangesAsync(ct);

        var resumeDto = await _resumeHandler.HandleAsync(new ResumeLessonAttemptQuery(attempt.Id), ct);
        return resumeDto!;
    }

    public async Task<ExerciseAnswerResultDto> HandleAsync(SubmitExerciseAnswerCommand command, CancellationToken ct = default)
    {
        var attempt = await _db.LessonAttempts
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == command.AttemptId, ct)
            ?? throw new NotFoundException($"Attempt with ID '{command.AttemptId}' not found.");

        if (_currentUser.UserId != null && !string.Equals(attempt.UserId, _currentUser.UserId, StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenException("You are not authorized to submit answers for this attempt.");

        var req = command.Request;

        // Fetch lesson exercise to get grading configuration
        var lesson = await _db.Lessons
            .Include(l => l.Exercises)
            .FirstOrDefaultAsync(l => l.Id == attempt.LessonId, ct)
            ?? throw new NotFoundException($"Lesson with ID '{attempt.LessonId}' not found.");

        var exercise = lesson.Exercises.FirstOrDefault(e => e.Id == req.ExerciseId)
            ?? throw new NotFoundException($"Exercise with ID '{req.ExerciseId}' not found in lesson.");

        string answerPayloadJson = req.AnswerPayload?.ToJsonString() ?? "{}";

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
            // Extract rubric from grading definition
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

        await _db.SaveChangesAsync(ct);

        var progress = CalculateProgress(attempt, lesson.Exercises);

        return new ExerciseAnswerResultDto(
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
    }

    public async Task<ExerciseAnswerResultDto> HandleAsync(RetryAnswerGradingCommand command, CancellationToken ct = default)
    {
        var attempt = await _db.LessonAttempts
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == command.AttemptId, ct)
            ?? throw new NotFoundException($"Attempt with ID '{command.AttemptId}' not found.");

        attempt.RetryGrading(command.AnswerId, command.AttemptRevision);
        await _db.SaveChangesAsync(ct);

        var answer = attempt.Answers.First(a => a.Id == command.AnswerId);
        var lesson = await _db.Lessons
            .Include(l => l.Exercises)
            .FirstOrDefaultAsync(l => l.Id == attempt.LessonId, ct);

        var progress = CalculateProgress(attempt, lesson?.Exercises ?? Array.Empty<ExerciseEntity>());

        return new ExerciseAnswerResultDto(
            AnswerId: answer.Id,
            AttemptId: attempt.Id,
            AttemptRevision: attempt.AttemptRevision,
            ExerciseId: answer.ExerciseId,
            SubQuestionId: answer.SubQuestionId,
            Status: answer.Status.ToString(),
            GradingMode: answer.GradingMode.ToString(),
            Score100: null,
            PointsEarned: null,
            MaxPoints: answer.MaxPoints,
            RubricCode: null,
            RubricVersion: null,
            CriteriaBreakdown: null,
            Strengths: null,
            Improvements: null,
            SuggestedAnswer: null,
            AttemptProgress: progress);
    }

    public async Task<LessonAttemptResumeDto> HandleAsync(CompleteLessonAttemptCommand command, CancellationToken ct = default)
    {
        var attempt = await _db.LessonAttempts
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == command.AttemptId, ct)
            ?? throw new NotFoundException($"Attempt with ID '{command.AttemptId}' not found.");

        attempt.Complete(_timeProvider.GetUtcNow());
        await _db.SaveChangesAsync(ct);

        var resumeDto = await _resumeHandler.HandleAsync(new ResumeLessonAttemptQuery(attempt.Id), ct);
        return resumeDto!;
    }

    private static AttemptProgressDto CalculateProgress(LessonAttemptEntity attempt, IEnumerable<ExerciseEntity> exercises)
    {
        int totalExercises = exercises.Count();
        int completedCount = attempt.Answers.Count(a => a.Status == SubmissionGradingStatus.Completed);
        int pendingCount = attempt.Answers.Count(a => a.Status is SubmissionGradingStatus.Pending or SubmissionGradingStatus.Grading);
        int failedCount = attempt.Answers.Count(a => a.Status is SubmissionGradingStatus.Failed or SubmissionGradingStatus.NotAssessable);

        return new AttemptProgressDto(
            CompletedExercisesCount: completedCount,
            TotalExercisesCount: totalExercises,
            PendingGradingCount: pendingCount,
            FailedGradingCount: failedCount,
            CurrentSubtotalPoints: attempt.CurrentSubtotalPoints,
            TotalMaxPoints: attempt.TotalMaxPoints,
            FinalScore100: attempt.FinalScore100);
    }
}
