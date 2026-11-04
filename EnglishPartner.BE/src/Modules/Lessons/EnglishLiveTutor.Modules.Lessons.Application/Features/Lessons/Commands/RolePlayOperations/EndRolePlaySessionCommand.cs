using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Common.Utilities;
using EnglishLiveTutor.Domain.Lessons.Repositories;

namespace EnglishLiveTutor.Application.Features.Lessons.Commands.RolePlayOperations;

public sealed record RolePlayTranscriptItemDto(string Role, string Message);

public sealed record RolePlayEndSessionResultDto(
    Guid SessionId,
    string Status,
    int TotalTurnsCompleted,
    List<RolePlayTranscriptItemDto> Transcript,
    string Message);

public sealed record EndRolePlaySessionCommand(
    Guid SessionId,
    string? OperationKey = null) : ICommand<RolePlayEndSessionResultDto>, IIdempotentCommand
{
    public string? IdempotencyKey => OperationKey;
}

public sealed class EndRolePlaySessionCommandHandler : ICommandHandler<EndRolePlaySessionCommand, RolePlayEndSessionResultDto>
{
    private readonly ILessonRolePlayRepository _repository;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyService _idempotency;

    public EndRolePlaySessionCommandHandler(
        ILessonRolePlayRepository repository,
        ICurrentUser currentUser,
        TimeProvider clock,
        IUnitOfWork unitOfWork,
        IIdempotencyService idempotency)
    {
        _repository = repository;
        _currentUser = currentUser;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
    }

    public async Task<RolePlayEndSessionResultDto> HandleAsync(EndRolePlaySessionCommand command, CancellationToken ct = default)
    {
        string? userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ForbiddenException("Authentication required to end a role-play session.");
        }

        string fingerprint = IdempotencyFingerprint.Compute(command, ("SessionId", command.SessionId));
        var replay = await _idempotency.CheckOrBeginOperationAsync(
            userId: userId,
            operationType: "RolePlay.EndSession",
            resourceScope: command.SessionId.ToString(),
            operationKey: command.OperationKey ?? string.Empty,
            requestFingerprint: fingerprint,
            ct: ct);

        if (replay != null)
        {
            if (replay.StatusCode == 409)
            {
                throw new ConflictException(replay.ErrorMessage ?? "Conflict processing role-play session end.");
            }

            var cached = System.Text.Json.JsonSerializer.Deserialize<RolePlayEndSessionResultDto>(replay.ResponsePayloadJson!);
            return cached!;
        }

        var session = await _repository.GetByIdWithTurnsAsync(command.SessionId, ct);
        if (session == null || session.IsDeleted)
        {
            if (!string.IsNullOrWhiteSpace(command.OperationKey))
            {
                await _idempotency.FailOperationAsync(userId, "RolePlay.EndSession", command.SessionId.ToString(), command.OperationKey, CancellationToken.None);
            }
            throw new NotFoundException("Role-play session not found.");
        }

        if (!string.Equals(session.UserId, userId, StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(command.OperationKey))
            {
                await _idempotency.FailOperationAsync(userId, "RolePlay.EndSession", command.SessionId.ToString(), command.OperationKey, CancellationToken.None);
            }
            throw new NotFoundException("Role-play session not found.");
        }

        var now = _clock.GetUtcNow();
        session.EndSession(now);
        await _unitOfWork.CommitChangesAsync(ct);

        var transcript = session.Turns
            .OrderBy(t => t.SequenceIndex)
            .Select(t => new RolePlayTranscriptItemDto(t.Role, t.Message))
            .ToList();

        var result = new RolePlayEndSessionResultDto(
            SessionId: session.Id,
            Status: "SessionEnded",
            TotalTurnsCompleted: session.CurrentTurnIndex,
            Transcript: transcript,
            Message: "Session ended successfully. Transcript saved.");

        if (!string.IsNullOrWhiteSpace(command.OperationKey))
        {
            await _idempotency.CompleteOperationAsync(
                userId: userId,
                operationType: "RolePlay.EndSession",
                resourceScope: command.SessionId.ToString(),
                operationKey: command.OperationKey,
                statusCode: 200,
                responsePayload: result,
                ct: ct);
        }

        return result;
    }
}
