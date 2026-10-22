using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;
using EnglishLiveTutor.Application.Features.VocabularyGames.Mapping;
using EnglishLiveTutor.Domain.VocabularyGames.Enums;
using EnglishLiveTutor.Domain.VocabularyGames.Repositories;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Commands.StartNextFallingWordRound;

public sealed class StartNextFallingWordRoundCommandHandler : ICommandHandler<StartNextFallingWordRoundCommand, ActiveFallingWordRoundDto?>
{
    private readonly IFallingWordGameRepository _repo;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public StartNextFallingWordRoundCommandHandler(
        IFallingWordGameRepository repo,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _repo = repo;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<ActiveFallingWordRoundDto?> HandleAsync(StartNextFallingWordRoundCommand command, CancellationToken ct = default)
    {
        string? userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("Vui lòng đăng nhập để tiếp tục trò chơi.");
        }

        var session = await _repo.GetByIdWithRoundsAsync(command.SessionId, ct)
            ?? throw new NotFoundException($"Phiên trò chơi với ID '{command.SessionId}' không tồn tại.");

        if (session.UserId != userId)
        {
            throw new ForbiddenException("Bạn không có quyền thao tác trên phiên trò chơi của người dùng khác.");
        }

        if (session.Status == FallingWordGameStatus.Completed || session.Status == FallingWordGameStatus.Abandoned)
        {
            return null;
        }

        var now = _timeProvider.GetUtcNow();
        var activeRound = session.StartNextRound(now);

        await _unitOfWork.CommitChangesAsync(ct);

        if (activeRound == null)
        {
            return null;
        }

        return FallingWordDtoMapper.ToActiveRoundDto(session, activeRound, now);
    }
}
