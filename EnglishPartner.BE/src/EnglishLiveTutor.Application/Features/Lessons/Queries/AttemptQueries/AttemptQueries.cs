using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.LessonAttempts.Enums;
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
                return new ExerciseLearnerDto(
                    Id: e.Id,
                    Key: e.Key,
                    Type: e.Type.ToString(),
                    Title: e.Title,
                    Instructions: e.Instructions,
                    Order: e.Order,
                    MaxPoints: e.MaxPoints,
                    GradingMode: e.GradingMode.ToString(),
                    Content: ParseJson(safeContent));
            }).ToList();

        var submittedAnswersMap = new Dictionary<string, SubmittedAnswerSummaryDto>();
        foreach (var ans in attempt.Answers)
        {
            string mapKey = ans.SubQuestionId.HasValue
                ? $"{ans.ExerciseId}_{ans.SubQuestionId.Value}"
                : ans.ExerciseId.ToString();

            submittedAnswersMap[mapKey] = new SubmittedAnswerSummaryDto(
                AnswerId: ans.Id,
                Status: ans.Status.ToString(),
                Score100: ans.Score100,
                PointsEarned: ans.PointsEarned,
                MaxPoints: ans.MaxPoints,
                Feedback: ans.GradingResultJson);
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
