using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Common.Utilities;
using EnglishLiveTutor.Domain.Lessons;
using EnglishLiveTutor.Domain.Lessons.Repositories;

namespace EnglishLiveTutor.Application.Features.Lessons.Commands.RolePlayOperations;

public sealed record RolePlayStartSessionResultDto(
    Guid SessionId,
    string Status,
    int CurrentTurnIndex,
    string AiRole,
    string InitialMessage,
    int MaxTurns);

public sealed record StartRolePlaySessionCommand(
    string Scenario,
    string? AiRole = null,
    string? InitialGreeting = null,
    int MaxTurns = 6,
    string? OperationKey = null) : ICommand<RolePlayStartSessionResultDto>, IIdempotentCommand
{
    public string? IdempotencyKey => OperationKey;
}

public sealed class StartRolePlaySessionCommandHandler : ICommandHandler<StartRolePlaySessionCommand, RolePlayStartSessionResultDto>
{
    private readonly ILessonRolePlayRepository _repository;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyService _idempotency;

    public StartRolePlaySessionCommandHandler(
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

    public async Task<RolePlayStartSessionResultDto> HandleAsync(StartRolePlaySessionCommand command, CancellationToken ct = default)
    {
        string? userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ForbiddenException("Authentication required to start a role-play session.");
        }

        string fingerprint = IdempotencyFingerprint.Compute(command);
        var replay = await _idempotency.CheckOrBeginOperationAsync(
            userId: userId,
            operationType: "RolePlay.StartSession",
            resourceScope: "Global",
            operationKey: command.OperationKey ?? string.Empty,
            requestFingerprint: fingerprint,
            ct: ct);

        if (replay != null)
        {
            if (replay.StatusCode == 409)
            {
                throw new ConflictException(replay.ErrorMessage ?? "Conflict processing role-play session start.");
            }

            var cached = System.Text.Json.JsonSerializer.Deserialize<RolePlayStartSessionResultDto>(replay.ResponsePayloadJson!);
            return cached!;
        }

        var now = _clock.GetUtcNow();
        var session = LessonRolePlaySessionEntity.Start(
            userId: userId,
            scenario: command.Scenario,
            aiRole: command.AiRole,
            initialGreeting: command.InitialGreeting,
            maxTurns: command.MaxTurns,
            now: now);

        try
        {
            _repository.Add(session);
            await _unitOfWork.CommitChangesAsync(ct);

            var result = new RolePlayStartSessionResultDto(
                SessionId: session.Id,
                Status: session.Status,
                CurrentTurnIndex: session.CurrentTurnIndex,
                AiRole: session.AiRole,
                InitialMessage: session.InitialGreeting,
                MaxTurns: session.MaxTurns);

            if (!string.IsNullOrWhiteSpace(command.OperationKey))
            {
                await _idempotency.CompleteOperationAsync(
                    userId: userId,
                    operationType: "RolePlay.StartSession",
                    resourceScope: "Global",
                    operationKey: command.OperationKey,
                    statusCode: 201,
                    responsePayload: result,
                    locationHeader: $"/api/lesson-roleplay/sessions/{session.Id}",
                    ct: ct);
            }

            return result;
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(command.OperationKey))
            {
                await _idempotency.FailOperationAsync(userId, "RolePlay.StartSession", "Global", command.OperationKey, CancellationToken.None);
            }
            throw;
        }
    }
}
