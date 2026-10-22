using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;
using EnglishLiveTutor.Application.Features.VocabularyGames.Mapping;
using EnglishLiveTutor.Domain.VocabularyGames.Repositories;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Queries.GetFallingWordSession;

public sealed class GetFallingWordSessionQueryHandler : IQueryHandler<GetFallingWordSessionQuery, FallingWordSessionDto?>
{
    private readonly IFallingWordGameRepository _repo;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public GetFallingWordSessionQueryHandler(
        IFallingWordGameRepository repo,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _repo = repo;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<FallingWordSessionDto?> HandleAsync(GetFallingWordSessionQuery query, CancellationToken ct = default)
    {
        string? userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("Vui lòng đăng nhập để xem thông tin phiên chơi.");
        }

        var session = await _repo.GetByIdWithRoundsAsync(query.SessionId, ct);
        if (session == null)
        {
            return null;
        }

        if (session.UserId != userId)
        {
            throw new ForbiddenException("Bạn không có quyền xem phiên trò chơi của người dùng khác.");
        }

        var now = _timeProvider.GetUtcNow();
        return FallingWordDtoMapper.ToSessionDto(session, now);
    }
}
