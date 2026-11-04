using System.Text.Json;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;
using EnglishLiveTutor.Application.Features.VocabularyGames.Mapping;
using EnglishLiveTutor.Application.Features.VocabularyGames.Ports;
using EnglishLiveTutor.Domain.VocabularyGames;
using EnglishLiveTutor.Domain.VocabularyGames.Entities;
using EnglishLiveTutor.Domain.VocabularyGames.Enums;
using EnglishLiveTutor.Domain.VocabularyGames.Repositories;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Commands.CreateFallingWordSession;

public sealed class CreateFallingWordSessionCommandHandler : ICommandHandler<CreateFallingWordSessionCommand, FallingWordSessionDto>
{
    private readonly IFallingWordGameRepository _repo;
    private readonly IFallingWordPoolReader _poolReader;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly IIdempotencyService _idempotencyService;

    public CreateFallingWordSessionCommandHandler(
        IFallingWordGameRepository repo,
        IFallingWordPoolReader poolReader,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IIdempotencyService idempotencyService)
    {
        _repo = repo;
        _poolReader = poolReader;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _idempotencyService = idempotencyService;
    }

    public async Task<FallingWordSessionDto> HandleAsync(CreateFallingWordSessionCommand command, CancellationToken ct = default)
    {
        string? userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("Vui lòng đăng nhập để bắt đầu trò chơi.");
        }

        var req = command.Request;
        string? operationKey = command.OperationKey?.Trim();

        if (!string.IsNullOrEmpty(operationKey))
        {
            string fingerprint = $"{req.Source}:{req.CefrLevel}:{req.PlayFilter}:{req.TimeLimitSeconds}:{req.RequestedCount}:{req.TimeZoneId}";
            var replayResult = await _idempotencyService.CheckOrBeginOperationAsync(
                userId: userId,
                operationType: "CreateFallingWordSession",
                resourceScope: "VocabularyGames",
                operationKey: operationKey,
                requestFingerprint: fingerprint,
                ct: ct);

            if (replayResult != null)
            {
                if (replayResult.IsReplay && !string.IsNullOrWhiteSpace(replayResult.ResponsePayloadJson))
                {
                    var cachedDto = JsonSerializer.Deserialize<FallingWordSessionDto>(replayResult.ResponsePayloadJson);
                    if (cachedDto != null)
                    {
                        return cachedDto;
                    }
                }

                throw new ConflictException(replayResult.ErrorMessage ?? "Thao tác tạo phiên chơi đang được xử lý hoặc trùng lặp khóa.");
            }
        }

        try
        {
            var source = Enum.Parse<FallingWordGameSource>(req.Source, true);
            var playFilter = Enum.Parse<FallingWordPlayFilter>(req.PlayFilter, true);

            var (selected, totalPoolCount, dailyPlanId, learningDate) = await _poolReader.SelectPoolForSessionAsync(
                userId: userId,
                source: source,
                cefrLevel: req.CefrLevel,
                playFilter: playFilter,
                requestedCount: req.RequestedCount,
                timeZoneId: req.TimeZoneId,
                ct: ct);

            if (selected.Count == 0)
            {
                throw new InvalidOperationException("Không tìm thấy từ vựng nào phù hợp với bộ lọc đã chọn để tạo ván chơi.");
            }

            var session = FallingWordGameSessionEntity.Create(
                userId: userId,
                source: source,
                cefrLevel: req.CefrLevel,
                playFilter: playFilter,
                timeLimitSeconds: req.TimeLimitSeconds,
                requestedCount: req.RequestedCount,
                dailyPlanId: dailyPlanId,
                learningDate: learningDate,
                timeZoneId: req.TimeZoneId);

            for (int i = 0; i < selected.Count; i++)
            {
                var item = selected[i];
                var round = new FallingWordGameRoundEntity(
                    sessionId: session.Id,
                    roundOrder: i + 1,
                    vocabularyId: item.VocabularyId,
                    senseId: item.SenseId,
                    targetWord: item.Word,
                    meaningVi: item.MeaningVi,
                    cefrLevel: item.CefrLevel,
                    dailyItemId: item.DailyItemId);

                session.AddRound(round);
            }

            await _repo.AddAsync(session, ct);
            await _unitOfWork.CommitChangesAsync(ct);

            var dto = FallingWordDtoMapper.ToSessionDto(session, _timeProvider.GetUtcNow());

            if (!string.IsNullOrEmpty(operationKey))
            {
                await _idempotencyService.CompleteOperationAsync(
                    userId: userId,
                    operationType: "CreateFallingWordSession",
                    resourceScope: "VocabularyGames",
                    operationKey: operationKey,
                    statusCode: 200,
                    responsePayload: dto,
                    ct: ct);
            }

            return dto;
        }
        catch when (!string.IsNullOrEmpty(operationKey))
        {
            await _idempotencyService.FailOperationAsync(
                userId: userId,
                operationType: "CreateFallingWordSession",
                resourceScope: "VocabularyGames",
                operationKey: operationKey,
                ct: ct);
            throw;
        }
    }
}
