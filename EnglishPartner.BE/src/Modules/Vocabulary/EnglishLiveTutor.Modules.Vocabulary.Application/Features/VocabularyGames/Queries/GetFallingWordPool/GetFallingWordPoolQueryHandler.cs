using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;
using EnglishLiveTutor.Application.Features.VocabularyGames.Ports;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Queries.GetFallingWordPool;

public sealed class GetFallingWordPoolQueryHandler : IQueryHandler<GetFallingWordPoolQuery, FallingWordPoolCountDto>
{
    private readonly IFallingWordPoolReader _poolReader;
    private readonly ICurrentUser _currentUser;

    public GetFallingWordPoolQueryHandler(
        IFallingWordPoolReader poolReader,
        ICurrentUser currentUser)
    {
        _poolReader = poolReader;
        _currentUser = currentUser;
    }

    public async Task<FallingWordPoolCountDto> HandleAsync(GetFallingWordPoolQuery query, CancellationToken ct = default)
    {
        string? userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("Vui lòng đăng nhập để xem thông tin từ vựng.");
        }

        return await _poolReader.GetPoolCountAsync(
            userId: userId,
            source: query.Source,
            cefrLevel: query.CefrLevel,
            playFilter: query.PlayFilter,
            timeZoneId: query.TimeZoneId,
            ct: ct);
    }
}
