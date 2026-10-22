using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.LessonAttempts.Enums;
using EnglishLiveTutor.Domain.Lessons.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Lessons.Queries.AttemptQueries;

public sealed record ResumeLessonAttemptQuery(Guid AttemptId) : IQuery<LessonAttemptResumeDto?>;

public sealed record ListLessonAttemptsQuery(
    string? UserId = null,
    int? Page = null,
    int? PageSize = null
) : IQuery<PagedResultDto<LessonAttemptSummaryDto>>;

public sealed class AttemptQueryHandlers :
    IQueryHandler<ResumeLessonAttemptQuery, LessonAttemptResumeDto?>,
    IQueryHandler<ListLessonAttemptsQuery, PagedResultDto<LessonAttemptSummaryDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILessonSanitizer _sanitizer;

    public AttemptQueryHandlers(
        IAppDbContext db,
        ICurrentUser currentUser,
        ILessonSanitizer sanitizer)
    {
        _db = db;
        _currentUser = currentUser;
        _sanitizer = sanitizer;
    }

    public async Task<LessonAttemptResumeDto?> HandleAsync(ResumeLessonAttemptQuery query, CancellationToken ct = default)
    {
        var attempt = await _db.LessonAttempts
            .AsNoTracking()
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == query.AttemptId, ct);

        if (attempt == null)
            return null;

        string? currentUserId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(currentUserId) || !string.Equals(attempt.UserId, currentUserId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("You are not authorized to view this lesson attempt.");
        }

        var lesson = await _db.Lessons
            .AsNoTracking()
            .Include(l => l.Exercises)
            .FirstOrDefaultAsync(l => l.Id == attempt.LessonId, ct)
            ?? throw new NotFoundException($"Lesson '{attempt.LessonId}' not found.");

        var learnerExercises = lesson.Exercises
            .OrderBy(e => e.Order)
            .Select(e =>
            {
                string safeContent = _sanitizer.SanitizeContentForLearner(e.Type, e.ContentJson);
                var contentNode = ParseJson(safeContent);

                if (e.Type == ExerciseType.MultipleChoice && contentNode is JsonObject mcObj)
                {
                    if (mcObj["selectionMode"] == null && !string.IsNullOrWhiteSpace(e.GradingDefinitionJson))
                    {
                        try
                        {
                            var g = JsonNode.Parse(e.GradingDefinitionJson);
                            var count = g?["correctOptionIds"]?.AsArray()?.Count ?? 0;
                            mcObj["selectionMode"] = count > 1 ? "Multiple" : "Single";
                        }
                        catch { }
                    }
                }
                else if (e.Type == ExerciseType.Flashcards && contentNode is JsonObject fcObj && fcObj["cards"] is JsonArray cardsArr)
                {
                    foreach (var c in cardsArr)
                    {
                        if (c is JsonObject cObj)
                        {
                            if (cObj["meaningVi"] == null && cObj["meaning"] != null)
                                cObj["meaningVi"] = cObj["meaning"]!.DeepClone();
                            if (cObj["exampleEn"] == null && cObj["example"] != null)
                                cObj["exampleEn"] = cObj["example"]!.DeepClone();
                        }
                    }
                }

                return new ExerciseLearnerDto(
                    Id: e.Id,
                    Key: e.Key,
                    Type: e.Type.ToString(),
                    Title: e.Title,
                    Instructions: e.Instructions,
                    Order: e.Order,
                    MaxPoints: e.MaxPoints,
                    GradingMode: e.GradingMode.ToString(),
                    Content: contentNode);
            }).ToList();

        var submittedAnswersMap = new Dictionary<string, SubmittedAnswerSummaryDto>();
        foreach (var ans in attempt.Answers)
        {
            string mapKey = ans.SubQuestionId.HasValue
                ? $"{ans.ExerciseId}_{ans.SubQuestionId.Value}"
                : ans.ExerciseId.ToString();

            JsonNode? answerPayload = null;
            if (!string.IsNullOrWhiteSpace(ans.AnswerPayloadJson))
            {
                try { answerPayload = JsonNode.Parse(ans.AnswerPayloadJson); } catch { }
            }

            string? feedback = ans.GradingResultJson;
            string? errorMessage = null;
            DictationAiReviewDto? aiReview = null;
            List<CriterionScoreBreakdownDto>? criteriaBreakdown = null;

            if (ans.Status is SubmissionGradingStatus.Failed or SubmissionGradingStatus.NotAssessable)
            {
                errorMessage = ans.GradingResultJson;
            }

            if (!string.IsNullOrWhiteSpace(ans.GradingResultJson))
            {
                string trimmed = ans.GradingResultJson.Trim();
                if (trimmed.StartsWith("["))
                {
                    try
                    {
                        criteriaBreakdown = System.Text.Json.JsonSerializer.Deserialize<List<CriterionScoreBreakdownDto>>(trimmed);
                        feedback = null; // Do not expose raw serialized criteria JSON array as plain text feedback
                    }
                    catch { }
                }
                else if (trimmed.StartsWith("{"))
                {
                    try
                    {
                        var resNode = JsonNode.Parse(trimmed);
                        if (resNode?["aiReview"] != null)
                        {
                            aiReview = System.Text.Json.JsonSerializer.Deserialize<DictationAiReviewDto>(resNode["aiReview"]!.ToJsonString());
                            feedback = resNode["explanation"]?.ToString();
                        }
                    }
                    catch { }
                }
            }

            var summary = new SubmittedAnswerSummaryDto(
                AnswerId: ans.Id,
                Status: ans.Status.ToString(),
                Score100: ans.Score100,
                PointsEarned: ans.PointsEarned,
                MaxPoints: ans.MaxPoints,
                Feedback: feedback,
                AnswerPayload: answerPayload,
                ErrorMessage: errorMessage,
                AiReview: aiReview,
                CriteriaBreakdown: criteriaBreakdown,
                ExerciseId: ans.ExerciseId,
                SubQuestionId: ans.SubQuestionId);

            submittedAnswersMap[mapKey] = summary;
            if (ans.SubQuestionId.HasValue && !submittedAnswersMap.ContainsKey(ans.ExerciseId.ToString()))
            {
                submittedAnswersMap[ans.ExerciseId.ToString()] = summary;
            }
        }

        int totalExercises = lesson.Exercises.Count;
        int completedCount = attempt.Answers.Count(a => a.Status == SubmissionGradingStatus.Completed);
        int pendingCount = attempt.Answers.Count(a => a.Status is SubmissionGradingStatus.Pending or SubmissionGradingStatus.Grading);
        int failedCount = attempt.Answers.Count(a => a.Status is SubmissionGradingStatus.Failed or SubmissionGradingStatus.NotAssessable);

        var progress = new AttemptProgressDto(
            CompletedExercisesCount: completedCount,
            TotalExercisesCount: totalExercises,
            PendingGradingCount: pendingCount,
            FailedGradingCount: failedCount,
            CurrentSubtotalPoints: attempt.CurrentSubtotalPoints,
            TotalMaxPoints: attempt.TotalMaxPoints,
            FinalScore100: attempt.FinalScore100);

        return new LessonAttemptResumeDto(
            AttemptId: attempt.Id,
            LessonId: lesson.Id,
            LessonTitle: lesson.Title,
            PinnedLessonVersion: attempt.PinnedLessonVersion,
            AttemptRevision: attempt.AttemptRevision,
            Status: attempt.Status.ToString(),
            StartedAt: attempt.StartedAtUtc,
            CompletedAt: attempt.CompletedAtUtc,
            Progress: progress,
            Exercises: learnerExercises,
            SubmittedAnswers: submittedAnswersMap);
    }

    public async Task<PagedResultDto<LessonAttemptSummaryDto>> HandleAsync(ListLessonAttemptsQuery query, CancellationToken ct = default)
    {
        string targetUserId = query.UserId ?? _currentUser.UserId ?? "anonymous";

        var dbQuery = (from a in _db.LessonAttempts.AsNoTracking()
                       join l in _db.Lessons.AsNoTracking() on a.LessonId equals l.Id
                       where a.UserId == targetUserId
                       orderby a.StartedAtUtc descending
                       select new LessonAttemptSummaryDto(
                           a.Id,
                           a.LessonId,
                           l.Title,
                           a.PinnedLessonVersion,
                           a.Status.ToString(),
                           a.StartedAtUtc,
                           a.CompletedAtUtc,
                           a.CurrentSubtotalPoints,
                           a.TotalMaxPoints,
                           a.FinalScore100
                       ));

        int totalCount = await dbQuery.CountAsync(ct);
        int page = Math.Max(1, query.Page ?? 1);
        int pageSize = Math.Clamp(query.PageSize ?? 20, 1, 500);

        var items = await dbQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        return new PagedResultDto<LessonAttemptSummaryDto>(items, totalCount, page, pageSize, totalPages);
    }

    private static JsonNode? ParseJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try { return JsonNode.Parse(json); }
        catch { return null; }
    }
}
