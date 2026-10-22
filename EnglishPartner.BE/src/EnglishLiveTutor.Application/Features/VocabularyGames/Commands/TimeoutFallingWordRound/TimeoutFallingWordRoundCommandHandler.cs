using System.Text.Json;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;
using EnglishLiveTutor.Application.Features.VocabularyGames.Mapping;
using EnglishLiveTutor.Domain.VocabularyGames.Repositories;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Commands.TimeoutFallingWordRound;

public sealed class TimeoutFallingWordRoundCommandHandler : ICommandHandler<TimeoutFallingWordRoundCommand, TerminalFallingWordRoundDto>
{
    private readonly IFallingWordGameRepository _repo;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly IIdempotencyService _idempotencyService;

    public TimeoutFallingWordRoundCommandHandler(
        IFallingWordGameRepository repo,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IIdempotencyService idempotencyService)
    {
        _repo = repo;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _idempotencyService = idempotencyService;
    }

    public async Task<TerminalFallingWordRoundDto> HandleAsync(TimeoutFallingWordRoundCommand command, CancellationToken ct = default)
    {
        string? userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("Vui lòng đăng nhập để xử lý kết thúc lượt.");
        }

        var session = await _repo.GetByIdWithRoundsAsync(command.SessionId, ct)
            ?? throw new NotFoundException($"Phiên trò chơi với ID '{command.SessionId}' không tồn tại.");

        if (session.UserId != userId)
        {
            throw new ForbiddenException("Bạn không có quyền thao tác trên phiên trò chơi của người dùng khác.");
        }

        string? operationKey = command.OperationKey?.Trim();
        if (!string.IsNullOrEmpty(operationKey))
        {
            string fingerprint = $"{command.SessionId}:{command.RoundId}:timeout";
            var replayResult = await _idempotencyService.CheckOrBeginOperationAsync(
                userId: userId,
                operationType: "TimeoutFallingWordRound",
                resourceScope: command.SessionId.ToString(),
                operationKey: operationKey,
                requestFingerprint: fingerprint,
                ct: ct);

            if (replayResult != null)
            {
                if (replayResult.IsReplay && !string.IsNullOrWhiteSpace(replayResult.ResponsePayloadJson))
                {
                    var cachedDto = JsonSerializer.Deserialize<TerminalFallingWordRoundDto>(replayResult.ResponsePayloadJson);
                    if (cachedDto != null)
                    {
                        return cachedDto;
                    }
                }

                throw new ConflictException(replayResult.ErrorMessage ?? "Thao tác kết thúc lượt đang được xử lý hoặc trùng lặp khóa.");
            }
        }

        try
        {
            var now = _timeProvider.GetUtcNow();
            var round = session.FinalizeTimeout(command.RoundId, now);

            await _unitOfWork.CommitChangesAsync(ct);

            var terminalDto = FallingWordDtoMapper.ToTerminalRoundDto(round);

            if (!string.IsNullOrEmpty(operationKey))
            {
                await _idempotencyService.CompleteOperationAsync(
                    userId: userId,
                    operationType: "TimeoutFallingWordRound",
                    resourceScope: command.SessionId.ToString(),
                    operationKey: operationKey,
                    statusCode: 200,
                    responsePayload: terminalDto,
                    ct: ct);
            }

            return terminalDto;
        }
        catch when (!string.IsNullOrEmpty(operationKey))
        {
            await _idempotencyService.FailOperationAsync(
                userId: userId,
                operationType: "TimeoutFallingWordRound",
                resourceScope: command.SessionId.ToString(),
                operationKey: operationKey,
                ct: ct);
            throw;
        }
    }
}
