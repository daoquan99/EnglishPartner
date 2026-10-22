using System.Text.Json;
using System.Text.Json.Nodes;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Common.Utilities;
using EnglishLiveTutor.Domain.Lessons.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnglishLiveTutor.Application.Features.Lessons.Commands.AttemptOperations;

public sealed record RequestDictationAiReviewCommand(
    Guid AttemptId,
    Guid AnswerId,
    RequestDictationAiReviewRequest Request,
    string? OperationKey = null
) : ICommand<DictationAiReviewDto>, IIdempotentCommand
{
    public string? IdempotencyKey => OperationKey;
}

public sealed class RequestDictationAiReviewCommandHandler : ICommandHandler<RequestDictationAiReviewCommand, DictationAiReviewDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDictationAiReviewer _reviewer;
    private readonly ILogger<RequestDictationAiReviewCommandHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyService _idempotency;

    public RequestDictationAiReviewCommandHandler(
        IAppDbContext db,
        ICurrentUser currentUser,
        IDictationAiReviewer reviewer,
        ILogger<RequestDictationAiReviewCommandHandler> logger,
        IUnitOfWork unitOfWork,
        IIdempotencyService idempotency)
    {
        _db = db;
        _currentUser = currentUser;
        _reviewer = reviewer;
        _logger = logger;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
    }

    public async Task<DictationAiReviewDto> HandleAsync(RequestDictationAiReviewCommand command, CancellationToken ct = default)
    {
        string? userId = _currentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ForbiddenException("Authentication required to request Dictation AI review.");
        }

        string fingerprint = IdempotencyFingerprint.Compute(
            command.Request,
            ("AttemptId", command.AttemptId),
            ("AnswerId", command.AnswerId));

        var replay = await _idempotency.CheckOrBeginOperationAsync(
            userId: userId,
            operationType: "LessonAttempt.DictationAiReview",
            resourceScope: command.AttemptId.ToString(),
            operationKey: command.OperationKey ?? string.Empty,
            requestFingerprint: fingerprint,
            ct: ct);

        if (replay != null)
        {
            if (replay.StatusCode == 409)
            {
                throw new ConflictException(replay.ErrorMessage ?? "Conflict requesting Dictation AI review.");
            }

            var cached = JsonSerializer.Deserialize<DictationAiReviewDto>(replay.ResponsePayloadJson!);
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
                await _idempotency.FailOperationAsync(userId, "LessonAttempt.DictationAiReview", command.AttemptId.ToString(), command.OperationKey, CancellationToken.None);
            }
            throw new ForbiddenException("You are not authorized to access this attempt.");
        }

        var answer = attempt.Answers.FirstOrDefault(a => a.Id == command.AnswerId)
            ?? throw new NotFoundException($"Answer with ID '{command.AnswerId}' not found in attempt.");

        var exercise = await _db.Exercises.FirstOrDefaultAsync(e => e.Id == answer.ExerciseId, ct)
            ?? throw new NotFoundException($"Exercise with ID '{answer.ExerciseId}' not found.");

        if (exercise.Type != ExerciseType.Dictation)
        {
            throw new InvalidOperationException("AI review is only available for Dictation exercises.");
        }

        var gNode = JsonNode.Parse(exercise.GradingDefinitionJson ?? "{}");
        string refTranscript = gNode?["referenceTranscript"]?.ToString() ?? gNode?["transcript"]?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(refTranscript))
        {
            throw new InvalidOperationException("Bài chép chính tả chưa có reference transcript để thực hiện review.");
        }

        var aNode = JsonNode.Parse(answer.AnswerPayloadJson ?? "{}");
        string learnerTranscript = aNode?["transcribedText"]?.ToString() ?? string.Empty;

        var reviewDto = await _reviewer.ReviewAsync(
            answer.Id,
            refTranscript,
            learnerTranscript,
            command.Request.AiConfig,
            ct);

        var resultObj = new JsonObject();
        if (!string.IsNullOrWhiteSpace(answer.GradingResultJson))
        {
            try
            {
                var existing = JsonNode.Parse(answer.GradingResultJson);
                if (existing is JsonObject exObj) resultObj = exObj;
                else resultObj["explanation"] = answer.GradingResultJson;
            }
            catch
            {
                resultObj["explanation"] = answer.GradingResultJson;
            }
        }
        resultObj["aiReview"] = JsonNode.Parse(JsonSerializer.Serialize(reviewDto));

        answer.AttachAiReview(resultObj.ToJsonString());

        try
        {
            await _unitOfWork.CommitChangesAsync(ct);

            if (!string.IsNullOrWhiteSpace(command.OperationKey))
            {
                await _idempotency.CompleteOperationAsync(
                    userId: userId,
                    operationType: "LessonAttempt.DictationAiReview",
                    resourceScope: command.AttemptId.ToString(),
                    operationKey: command.OperationKey,
                    statusCode: 200,
                    responsePayload: reviewDto,
                    ct: ct);
            }

            return reviewDto;
        }
        catch
        {
            if (!string.IsNullOrWhiteSpace(command.OperationKey))
            {
                await _idempotency.FailOperationAsync(userId, "LessonAttempt.DictationAiReview", command.AttemptId.ToString(), command.OperationKey, CancellationToken.None);
            }
            throw;
        }
    }
}
