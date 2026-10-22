using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;
using EnglishLiveTutor.Application.Features.VocabularyGames.Mapping;
using EnglishLiveTutor.Domain.VocabularyGames.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Queries.ListFallingWordSessionRounds;

public sealed class ListFallingWordSessionRoundsQueryHandler : IQueryHandler<ListFallingWordSessionRoundsQuery, PagedResultDto<TerminalFallingWordRoundDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListFallingWordSessionRoundsQueryHandler(
        IAppDbContext db,
        ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResultDto<TerminalFallingWordRoundDto>> HandleAsync(ListFallingWordSessionRoundsQuery query, CancellationToken ct = default)
    {
        string? userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("Vui lòng đăng nhập để xem chi tiết các lượt chơi.");
        }

        var session = await _db.FallingWordGameSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == query.SessionId, ct)
            ?? throw new NotFoundException($"Phiên trò chơi với ID '{query.SessionId}' không tồn tại.");

        if (session.UserId != userId)
        {
            throw new ForbiddenException("Bạn không có quyền xem chi tiết phiên chơi của người dùng khác.");
        }

        bool isTerminalSession = session.Status == FallingWordGameStatus.Completed || session.Status == FallingWordGameStatus.Abandoned;

        var roundsQuery = _db.FallingWordGameRounds
            .AsNoTracking()
            .Where(r => r.SessionId == query.SessionId);

        // Security: only show terminal rounds unless the session itself is terminal
        if (!isTerminalSession)
        {
            roundsQuery = roundsQuery.Where(r =>
                r.Status == FallingWordRoundStatus.Correct ||
                r.Status == FallingWordRoundStatus.Incorrect ||
                r.Status == FallingWordRoundStatus.TimedOut ||
                r.Status == FallingWordRoundStatus.Abandoned);
        }

        return await PagedResultDto<TerminalFallingWordRoundDto>.CreateProjectedAsync(
            roundsQuery.OrderBy(r => r.RoundOrder),
            (rounds, _) => Task.FromResult(rounds.Select(FallingWordDtoMapper.ToTerminalRoundDto).ToList()),
            query.Page,
            query.PageSize,
            ct);
    }
}
