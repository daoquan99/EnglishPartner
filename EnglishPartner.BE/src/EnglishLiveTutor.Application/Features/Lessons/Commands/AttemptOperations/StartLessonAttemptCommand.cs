using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Common.Utilities;
using EnglishLiveTutor.Application.Features.Lessons.Queries.AttemptQueries;
using EnglishLiveTutor.Domain.LessonAttempts;
using EnglishLiveTutor.Domain.Lessons.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Lessons.Commands.AttemptOperations;

public sealed record StartLessonAttemptCommand(
    Guid LessonId,
    string? OperationKey = null) : ICommand<LessonAttemptResumeDto>, IIdempotentCommand
{
    public string? IdempotencyKey => OperationKey;
}

public sealed class StartLessonAttemptCommandHandler : ICommandHandler<StartLessonAttemptCommand, LessonAttemptResumeDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyService _idempotency;
    private readonly IQueryHandler<ResumeLessonAttemptQuery, LessonAttemptResumeDto?> _resumeHandler;

    public StartLessonAttemptCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        IIdempotencyService idempotency,
        IQueryHandler<ResumeLessonAttemptQuery, LessonAttemptResumeDto?> resumeHandler)
    {
        _db = db;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
        _resumeHandler = resumeHandler;
    }

    public async Task<LessonAttemptResumeDto> HandleAsync(StartLessonAttemptCommand command, CancellationToken ct = default)
    {
        string? userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ForbiddenException("Authentication required to start a lesson attempt.");
        }

        string fingerprint = IdempotencyFingerprint.Compute(command, ("LessonId", command.LessonId));
        var replay = await _idempotency.CheckOrBeginOperationAsync(
            userId: userId,
            operationType: "LessonAttempt.Start",
            resourceScope: command.LessonId.ToString(),
            operationKey: command.OperationKey ?? string.Empty,
            requestFingerprint: fingerprint,
            ct: ct);

        if (replay != null)
        {
            if (replay.StatusCode == 409)
            {
                throw new ConflictException(replay.ErrorMessage ?? "Conflict starting lesson attempt.");
            }

            var cached = System.Text.Json.JsonSerializer.Deserialize<LessonAttemptResumeDto>(replay.ResponsePayloadJson!);
            return cached!;
        }

        var lesson = await _db.Lessons
            .Include(l => l.Exercises)
            .FirstOrDefaultAsync(l => l.Id == command.LessonId, ct)
            ?? throw new NotFoundException($"Lesson with ID '{command.LessonId}' not found.");

        if (lesson.Status != LessonStatus.Published)
            throw new InvalidOperationException("Cannot start an attempt for an un-published lesson.");

        decimal totalMaxPoints = lesson.Exercises
            .Where(e => e.GradingMode != GradingMode.SelfAssessment)
            .Sum(e => e.MaxPoints);

        var attempt = LessonAttemptEntity.Start(
            lessonId: lesson.Id,
            userId: userId,
            pinnedLessonVersion: lesson.Version,
            totalMaxPoints: totalMaxPoints);

        try
        {
            _db.LessonAttempts.Add(attempt);
            await _unitOfWork.CommitChangesAsync(ct);

            var resumeDto = await _resumeHandler.HandleAsync(new ResumeLessonAttemptQuery(attempt.Id), ct);

            if (!string.IsNullOrWhiteSpace(command.OperationKey))
            {
                await _idempotency.CompleteOperationAsync(
                    userId: userId,
                    operationType: "LessonAttempt.Start",
                    resourceScope: command.LessonId.ToString(),
                    operationKey: command.OperationKey,
                    statusCode: 201,
                    responsePayload: resumeDto,
                    locationHeader: $"/api/lesson-attempts/{attempt.Id}",
                    ct: ct);
            }

            return resumeDto!;
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(command.OperationKey))
            {
                await _idempotency.FailOperationAsync(userId, "LessonAttempt.Start", command.LessonId.ToString(), command.OperationKey, CancellationToken.None);
            }
            throw;
        }
    }
}
