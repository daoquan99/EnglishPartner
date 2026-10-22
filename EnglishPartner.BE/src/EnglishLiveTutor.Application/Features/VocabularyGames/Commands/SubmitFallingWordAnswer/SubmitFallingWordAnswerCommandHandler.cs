using System.Text.Json;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;
using EnglishLiveTutor.Application.Features.VocabularyGames.Mapping;
using EnglishLiveTutor.Domain.VocabularyGames.Enums;
using EnglishLiveTutor.Domain.VocabularyGames.Repositories;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Commands.SubmitFallingWordAnswer;

public sealed class SubmitFallingWordAnswerCommandHandler : ICommandHandler<SubmitFallingWordAnswerCommand, SubmitFallingWordAnswerResponse>
{
    private readonly IFallingWordGameRepository _repo;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly IIdempotencyService _idempotencyService;

    public SubmitFallingWordAnswerCommandHandler(
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

    public async Task<SubmitFallingWordAnswerResponse> HandleAsync(SubmitFallingWordAnswerCommand command, CancellationToken ct = default)
    {
        string? userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("Vui lòng đăng nhập để nộp câu trả lời.");
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
            string fingerprint = $"{command.SessionId}:{command.RoundId}:{command.Request.AnswerText}";
            var replayResult = await _idempotencyService.CheckOrBeginOperationAsync(
                userId: userId,
                operationType: "SubmitFallingWordAnswer",
                resourceScope: command.SessionId.ToString(),
                operationKey: operationKey,
                requestFingerprint: fingerprint,
                ct: ct);

            if (replayResult != null)
            {
                if (replayResult.IsReplay && !string.IsNullOrWhiteSpace(replayResult.ResponsePayloadJson))
                {
                    var cachedResponse = JsonSerializer.Deserialize<SubmitFallingWordAnswerResponse>(replayResult.ResponsePayloadJson);
                    if (cachedResponse != null)
                    {
                        return cachedResponse;
                    }
                }

                throw new ConflictException(replayResult.ErrorMessage ?? "Thao tác nộp bài đang được xử lý hoặc trùng lặp khóa.");
            }
        }

        try
        {
            var now = _timeProvider.GetUtcNow();
            var round = session.SubmitAnswer(command.RoundId, command.Request.AnswerText!, now);

            await _unitOfWork.CommitChangesAsync(ct);

            var terminalDto = FallingWordDtoMapper.ToTerminalRoundDto(round);
            bool isSessionCompleted = session.Status == FallingWordGameStatus.Completed;

            var response = new SubmitFallingWordAnswerResponse(
                RoundResult: terminalDto,
                IsSessionCompleted: isSessionCompleted,
                CorrectCount: session.CorrectCount,
                IncorrectCount: session.IncorrectCount,
                TimedOutCount: session.TimedOutCount,
                CurrentStreak: session.CurrentStreak,
                BestStreak: session.BestStreak,
                Accuracy: session.Accuracy);

            if (!string.IsNullOrEmpty(operationKey))
            {
                await _idempotencyService.CompleteOperationAsync(
                    userId: userId,
                    operationType: "SubmitFallingWordAnswer",
                    resourceScope: command.SessionId.ToString(),
                    operationKey: operationKey,
                    statusCode: 200,
                    responsePayload: response,
                    ct: ct);
            }

            return response;
        }
        catch when (!string.IsNullOrEmpty(operationKey))
        {
            await _idempotencyService.FailOperationAsync(
                userId: userId,
                operationType: "SubmitFallingWordAnswer",
                resourceScope: command.SessionId.ToString(),
                operationKey: operationKey,
                ct: ct);
            throw;
        }
    }
}
