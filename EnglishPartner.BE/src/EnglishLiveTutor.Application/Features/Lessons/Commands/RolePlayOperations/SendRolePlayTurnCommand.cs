using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Common.Utilities;
using EnglishLiveTutor.Domain.Lessons.Repositories;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;

namespace EnglishLiveTutor.Application.Features.Lessons.Commands.RolePlayOperations;

public sealed record RolePlayMessageDto(string Message);

public sealed record RolePlayTurnResultDto(
    Guid SessionId,
    int TurnIndex,
    RolePlayMessageDto UserTurn,
    RolePlayMessageDto AiTurn,
    bool IsCompleted,
    int RemainingTurns);

public sealed record SendRolePlayTurnCommand(
    Guid SessionId,
    int ClientTurnIndex,
    string UserMessage,
    string? UserAudioAssetId = null,
    string? OperationKey = null) : ICommand<RolePlayTurnResultDto>, IIdempotentCommand
{
    public string? IdempotencyKey => OperationKey;
}

public sealed class SendRolePlayTurnCommandHandler : ICommandHandler<SendRolePlayTurnCommand, RolePlayTurnResultDto>
{
    private readonly ILessonRolePlayRepository _repository;
    private readonly IAudioRecordingRepository _audioRepository;
    private readonly ILessonMediaStorage _mediaStorage;
    private readonly IRolePlayReplyProvider _replyProvider;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyService _idempotency;

    public SendRolePlayTurnCommandHandler(
        ILessonRolePlayRepository repository,
        IAudioRecordingRepository audioRepository,
        ILessonMediaStorage mediaStorage,
        IRolePlayReplyProvider replyProvider,
        ICurrentUser currentUser,
        TimeProvider clock,
        IUnitOfWork unitOfWork,
        IIdempotencyService idempotency)
    {
        _repository = repository;
        _audioRepository = audioRepository;
        _mediaStorage = mediaStorage;
        _replyProvider = replyProvider;
        _currentUser = currentUser;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
    }

    public async Task<RolePlayTurnResultDto> HandleAsync(SendRolePlayTurnCommand command, CancellationToken ct = default)
    {
        string? userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ForbiddenException("Authentication required to interact in role-play.");
        }

        string fingerprint = IdempotencyFingerprint.Compute(
            command,
            ("SessionId", command.SessionId),
            ("ClientTurnIndex", command.ClientTurnIndex));

        var replay = await _idempotency.CheckOrBeginOperationAsync(
            userId: userId,
            operationType: "RolePlay.SendTurn",
            resourceScope: command.SessionId.ToString(),
            operationKey: command.OperationKey ?? string.Empty,
            requestFingerprint: fingerprint,
            ct: ct);

        if (replay != null)
        {
            if (replay.StatusCode == 409)
            {
                throw new ConflictException(replay.ErrorMessage ?? "Conflict processing role-play turn.");
            }

            var cached = System.Text.Json.JsonSerializer.Deserialize<RolePlayTurnResultDto>(replay.ResponsePayloadJson!);
            return cached!;
        }

        var session = await _repository.GetByIdWithTurnsAsync(command.SessionId, ct);
        if (session == null || session.IsDeleted)
        {
            if (!string.IsNullOrWhiteSpace(command.OperationKey))
            {
                await _idempotency.FailOperationAsync(userId, "RolePlay.SendTurn", command.SessionId.ToString(), command.OperationKey, CancellationToken.None);
            }
            throw new NotFoundException("Role-play session not found.");
        }

        if (!string.Equals(session.UserId, userId, StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(command.OperationKey))
            {
                await _idempotency.FailOperationAsync(userId, "RolePlay.SendTurn", command.SessionId.ToString(), command.OperationKey, CancellationToken.None);
            }
            throw new NotFoundException("Role-play session not found.");
        }

        // Validate audio asset if provided
        Guid? assetGuid = null;
        if (!string.IsNullOrWhiteSpace(command.UserAudioAssetId))
        {
            if (!Guid.TryParse(command.UserAudioAssetId, out var parsedGuid))
            {
                throw new ArgumentException("Invalid UserAudioAssetId format.");
            }

            var audio = await _audioRepository.GetByIdAsync(parsedGuid, ct);
            if (audio == null || audio.IsDeleted || !string.Equals(audio.UserId, userId, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Audio recording not found or does not belong to the current user.");
            }

            bool bytesExist = await _mediaStorage.ExistsAsync(audio.StorageKey, ct);
            if (!bytesExist)
            {
                throw new InvalidOperationException("Audio recording has no stored audio data.");
            }

            assetGuid = parsedGuid;
        }

        var now = _clock.GetUtcNow();

        // 1. Reserve turn in session to prevent concurrent executions
        session.ReserveTurn(command.ClientTurnIndex, now, TimeSpan.FromSeconds(45));
        await _unitOfWork.CommitChangesAsync(ct);

        // 2. Prepare history for reply provider
        var history = session.Turns
            .OrderBy(t => t.SequenceIndex)
            .Select(t => new RolePlayHistoryItem(t.Role, t.Message))
            .Concat(new[] { new RolePlayHistoryItem("user", command.UserMessage) })
            .ToList();

        string aiReply;
        try
        {
            aiReply = await _replyProvider.GenerateReplyAsync(
                scenario: session.Scenario,
                aiRole: session.AiRole,
                conversationHistory: history,
                currentTurn: command.ClientTurnIndex,
                maxTurns: session.MaxTurns,
                ct: ct);
        }
        catch
        {
            // Release reservation so caller can retry immediately
            session.ReleaseReservation(_clock.GetUtcNow());
            await _unitOfWork.CommitChangesAsync(CancellationToken.None);

            if (!string.IsNullOrWhiteSpace(command.OperationKey))
            {
                await _idempotency.FailOperationAsync(userId, "RolePlay.SendTurn", command.SessionId.ToString(), command.OperationKey, CancellationToken.None);
            }
            throw;
        }

        // 3. Commit turn in domain aggregate
        session.CommitTurn(
            clientTurnIndex: command.ClientTurnIndex,
            userMessage: command.UserMessage,
            userAudioAssetId: assetGuid,
            aiReply: aiReply,
            now: _clock.GetUtcNow());

        await _unitOfWork.CommitChangesAsync(ct);

        bool isCompleted = session.CurrentTurnIndex >= session.MaxTurns || session.Status == "SessionEnded";
        var result = new RolePlayTurnResultDto(
            SessionId: session.Id,
            TurnIndex: session.CurrentTurnIndex,
            UserTurn: new RolePlayMessageDto(command.UserMessage),
            AiTurn: new RolePlayMessageDto(aiReply),
            IsCompleted: isCompleted,
            RemainingTurns: Math.Max(0, session.MaxTurns - session.CurrentTurnIndex));

        if (!string.IsNullOrWhiteSpace(command.OperationKey))
        {
            await _idempotency.CompleteOperationAsync(
                userId: userId,
                operationType: "RolePlay.SendTurn",
                resourceScope: command.SessionId.ToString(),
                operationKey: command.OperationKey,
                statusCode: 200,
                responsePayload: result,
                ct: ct);
        }

        return result;
    }
}
