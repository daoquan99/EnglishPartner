using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Lessons.Services;
using EnglishLiveTutor.Application.Modules.Lessons;
using EnglishLiveTutor.Domain.LessonAttempts.Enums;
using EnglishLiveTutor.Domain.Common.Exercises;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Lessons.Queries.AttemptQueries;

public sealed record ResumeLessonAttemptQuery(Guid AttemptId) : IQuery<LessonAttemptResumeDto?>;

public sealed record ListLessonAttemptsQuery(
    int? Page = null,
    int? PageSize = null
) : IQuery<PagedResultDto<LessonAttemptSummaryDto>>;

public sealed class AttemptQueryHandlers :
    IQueryHandler<ResumeLessonAttemptQuery, LessonAttemptResumeDto?>,
    IQueryHandler<ListLessonAttemptsQuery, PagedResultDto<LessonAttemptSummaryDto>>
{
    private readonly ILessonsDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILessonAttemptResumeBuilder _resumeBuilder;

    public AttemptQueryHandlers(
        ILessonsDbContext db,
        ICurrentUser currentUser,
        ILessonAttemptResumeBuilder resumeBuilder)
    {
        _db = db;
        _currentUser = currentUser;
        _resumeBuilder = resumeBuilder;
    }

    public async Task<LessonAttemptResumeDto?> HandleAsync(ResumeLessonAttemptQuery query, CancellationToken ct = default)
    {
        return await _resumeBuilder.BuildAsync(query.AttemptId, ct);
    }

    public async Task<PagedResultDto<LessonAttemptSummaryDto>> HandleAsync(ListLessonAttemptsQuery query, CancellationToken ct = default)
    {
        string? targetUserId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(targetUserId))
        {
            throw new ForbiddenException("User must be authenticated to list lesson attempts.");
        }

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

        return await PagedResultDto<LessonAttemptSummaryDto>.CreateAsync(
            dbQuery,
            query.Page ?? 1, // Lesson lists keep their historical default page (20 rows) when the client sends no paging.
            query.PageSize,
            ct);
    }
}
