using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;
using EnglishLiveTutor.Application.Features.VocabularyGames.Mapping;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Queries.ListFallingWordSessions;

public sealed class ListFallingWordSessionsQueryHandler : IQueryHandler<ListFallingWordSessionsQuery, PagedResultDto<FallingWordSessionSummaryDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListFallingWordSessionsQueryHandler(
        IAppDbContext db,
        ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResultDto<FallingWordSessionSummaryDto>> HandleAsync(ListFallingWordSessionsQuery query, CancellationToken ct = default)
    {
        string? userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("Vui lòng đăng nhập để xem danh sách phiên chơi.");
        }

        var sessionsQuery = _db.FallingWordGameSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId);

        if (query.Source.HasValue)
        {
            sessionsQuery = sessionsQuery.Where(s => s.Source == query.Source.Value);
        }

        if (query.Status.HasValue)
        {
            sessionsQuery = sessionsQuery.Where(s => s.Status == query.Status.Value);
        }

        return await PagedResultDto<FallingWordSessionSummaryDto>.CreateProjectedAsync(
            sessionsQuery.OrderByDescending(s => s.CreatedAtUtc),
            (sessions, _) => Task.FromResult(sessions.Select(FallingWordDtoMapper.ToSummaryDto).ToList()),
            query.Page,
            query.PageSize,
            ct);
    }
}
