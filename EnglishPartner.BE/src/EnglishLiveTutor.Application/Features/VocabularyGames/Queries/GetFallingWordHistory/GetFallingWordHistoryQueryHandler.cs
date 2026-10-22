using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;
using EnglishLiveTutor.Application.Features.VocabularyGames.Mapping;
using EnglishLiveTutor.Domain.VocabularyGames.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Queries.GetFallingWordHistory;

public sealed class GetFallingWordHistoryQueryHandler : IQueryHandler<GetFallingWordHistoryQuery, PagedResultDto<WordFallingHistoryItemDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetFallingWordHistoryQueryHandler(
        IAppDbContext db,
        ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResultDto<WordFallingHistoryItemDto>> HandleAsync(GetFallingWordHistoryQuery query, CancellationToken ct = default)
    {
        string? userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("Vui lòng đăng nhập để xem lịch sử chơi từ vựng.");
        }

        var historyQuery = from r in _db.FallingWordGameRounds.AsNoTracking()
                           join s in _db.FallingWordGameSessions.AsNoTracking() on r.SessionId equals s.Id
                           where s.UserId == userId &&
                                 (r.Status == FallingWordRoundStatus.Correct ||
                                  r.Status == FallingWordRoundStatus.Incorrect ||
                                  r.Status == FallingWordRoundStatus.TimedOut ||
                                  r.Status == FallingWordRoundStatus.Abandoned)
                           select new { Round = r, Session = s };

        if (query.SenseId.HasValue && query.SenseId.Value != Guid.Empty)
        {
            historyQuery = historyQuery.Where(x => x.Round.SenseId == query.SenseId.Value);
        }

        return await PagedResultDto<WordFallingHistoryItemDto>.CreateProjectedAsync(
            historyQuery.OrderByDescending(x => x.Round.StartedAtUtc ?? x.Round.CreatedAtUtc).ThenByDescending(x => x.Round.Id),
            (items, _) => Task.FromResult(items.Select(x => FallingWordDtoMapper.ToHistoryItemDto(x.Round, x.Session.Source.ToString(), x.Session.TimeLimitSeconds)).ToList()),
            query.Page,
            query.PageSize,
            ct);
    }
}
