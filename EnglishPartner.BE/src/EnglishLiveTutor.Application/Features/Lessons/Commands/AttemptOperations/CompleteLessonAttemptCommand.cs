using System.Text.Json;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Common.Utilities;
using EnglishLiveTutor.Application.Features.Lessons.Queries.AttemptQueries;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Lessons.Commands.AttemptOperations;

public sealed record CompleteLessonAttemptCommand(
    Guid AttemptId,
    string? OperationKey = null
) : ICommand<LessonAttemptResumeDto>, IIdempotentCommand
{
    public string? IdempotencyKey => OperationKey;
}

public sealed class CompleteLessonAttemptCommandHandler : ICommandHandler<CompleteLessonAttemptCommand, LessonAttemptResumeDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyService _idempotency;
    private readonly IQueryHandler<ResumeLessonAttemptQuery, LessonAttemptResumeDto?> _resumeHandler;

    public CompleteLessonAttemptCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork,
        IIdempotencyService idempotency,
        IQueryHandler<ResumeLessonAttemptQuery, LessonAttemptResumeDto?> resumeHandler)
    {
        _db = db;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
        _resumeHandler = resumeHandler;
    }

    public async Task<LessonAttemptResumeDto> HandleAsync(CompleteLessonAttemptCommand command, CancellationToken ct = default)
    {
        string? userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ForbiddenException("Authentication required to complete lesson attempt.");
        }

        string fingerprint = IdempotencyFingerprint.Compute(command, ("AttemptId", command.AttemptId));
        var replay = await _idempotency.CheckOrBeginOperationAsync(
            userId: userId,
            operationType: "LessonAttempt.Complete",
            resourceScope: command.AttemptId.ToString(),
            operationKey: command.OperationKey ?? string.Empty,
            requestFingerprint: fingerprint,
            ct: ct);

        if (replay != null)
        {
            if (replay.StatusCode == 409)
            {
                throw new ConflictException(replay.ErrorMessage ?? "Conflict completing lesson attempt.");
            }

            var cached = JsonSerializer.Deserialize<LessonAttemptResumeDto>(replay.ResponsePayloadJson!);
            return cached!;
        }

        var attempt = await _db.LessonAttempts
            .Include(a => a.Answers)
            .FirstOrDefaultAsync(a => a.Id == command.AttemptId, ct)
            ?? throw new NotFoundException($"Attempt with ID '{command.AttemptId}' not found.");

        if (!string.Equals(attempt.UserId, userId, StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(command.OperationKey))
            {
                await _idempotency.FailOperationAsync(userId, "LessonAttempt.Complete", command.AttemptId.ToString(), command.OperationKey, CancellationToken.None);
            }
            throw new ForbiddenException("You are not authorized to complete this attempt.");
        }

        attempt.Complete(_timeProvider.GetUtcNow());

        try
        {
            await _unitOfWork.CommitChangesAsync(ct);

            var resumeDto = await _resumeHandler.HandleAsync(new ResumeLessonAttemptQuery(attempt.Id), ct);

            if (!string.IsNullOrWhiteSpace(command.OperationKey))
            {
                await _idempotency.CompleteOperationAsync(
                    userId: userId,
                    operationType: "LessonAttempt.Complete",
                    resourceScope: command.AttemptId.ToString(),
                    operationKey: command.OperationKey,
                    statusCode: 200,
                    responsePayload: resumeDto,
                    ct: ct);
            }

            return resumeDto!;
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(command.OperationKey))
            {
                await _idempotency.FailOperationAsync(userId, "LessonAttempt.Complete", command.AttemptId.ToString(), command.OperationKey, CancellationToken.None);
            }
            throw;
        }
    }
}
