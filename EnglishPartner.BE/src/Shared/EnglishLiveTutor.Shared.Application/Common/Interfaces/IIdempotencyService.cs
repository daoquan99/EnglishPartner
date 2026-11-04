namespace EnglishLiveTutor.Application.Common.Interfaces;

public sealed record IdempotencyReplayResult(
    bool IsReplay,
    int StatusCode,
    string? ResponsePayloadJson,
    string? LocationHeader = null,
    string? ErrorMessage = null);

public interface IIdempotencyService
{
    /// <summary>
    /// Evaluates whether an operation with (userId, operationType, resourceScope, operationKey)
    /// has already completed or is currently running.
    /// Returns an IdempotencyReplayResult if replay or conflict occurs, or null if the caller should proceed.
    /// </summary>
    Task<IdempotencyReplayResult?> CheckOrBeginOperationAsync(
        string userId,
        string operationType,
        string resourceScope,
        string operationKey,
        string requestFingerprint,
        TimeSpan? ttl = null,
        CancellationToken ct = default);

    /// <summary>
    /// Marks the idempotency record as Completed with the serialized response payload and status code.
    /// </summary>
    Task CompleteOperationAsync(
        string userId,
        string operationType,
        string resourceScope,
        string operationKey,
        int statusCode,
        object? responsePayload,
        string? locationHeader = null,
        CancellationToken ct = default);

    /// <summary>
    /// Removes or marks failed a pending lease when an unhandled exception or cancellation occurs.
    /// </summary>
    Task FailOperationAsync(
        string userId,
        string operationType,
        string resourceScope,
        string operationKey,
        CancellationToken ct = default);
}
