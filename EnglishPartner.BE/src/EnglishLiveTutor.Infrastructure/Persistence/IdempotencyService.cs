using System.Text.Json;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnglishLiveTutor.Infrastructure.Persistence;

public sealed class IdempotencyService : IIdempotencyService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan PendingLeaseDuration = TimeSpan.FromSeconds(30);

    private readonly AppDbContext _db;
    private readonly TimeProvider _clock;
    private readonly ILogger<IdempotencyService> _logger;

    public IdempotencyService(
        AppDbContext db,
        TimeProvider? clock = null,
        ILogger<IdempotencyService>? logger = null)
    {
        _db = db;
        _clock = clock ?? TimeProvider.System;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<IdempotencyService>.Instance;
    }

    public async Task<IdempotencyReplayResult?> CheckOrBeginOperationAsync(
        string userId,
        string operationType,
        string resourceScope,
        string operationKey,
        string requestFingerprint,
        TimeSpan? ttl = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(operationKey))
        {
            return null; // Key omitted, bypass idempotency
        }

        string cleanUser = userId?.Trim() ?? string.Empty;
        string cleanOp = operationType?.Trim() ?? string.Empty;
        string cleanScope = resourceScope?.Trim() ?? string.Empty;
        string cleanKey = operationKey.Trim();
        string cleanFingerprint = requestFingerprint?.Trim() ?? string.Empty;

        var now = _clock.GetUtcNow();
        var effectiveTtl = ttl ?? DefaultTtl;

        var existing = await _db.IdempotencyRecords
            .FirstOrDefaultAsync(r =>
                r.UserId == cleanUser &&
                r.OperationType == cleanOp &&
                r.ResourceScope == cleanScope &&
                r.OperationKey == cleanKey,
                ct);

        if (existing != null)
        {
            // 1. Conflict check: payload fingerprint mismatch
            if (!string.Equals(existing.RequestFingerprint, cleanFingerprint, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Idempotency key conflict for user {UserId}, op {Op}: payload fingerprint mismatch.", cleanUser, cleanOp);
                return new IdempotencyReplayResult(
                    IsReplay: true,
                    StatusCode: 409,
                    ResponsePayloadJson: null,
                    ErrorMessage: "IDEMPOTENCY_KEY_PAYLOAD_MISMATCH: Operation key was previously used with a different request payload.");
            }

            // 2. Replay completed result
            if (string.Equals(existing.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Replaying completed idempotent result for user {UserId}, op {Op}, key {Key}.", cleanUser, cleanOp, cleanKey);
                return new IdempotencyReplayResult(
                    IsReplay: true,
                    StatusCode: existing.ResponseStatusCode,
                    ResponsePayloadJson: existing.ResponsePayloadJson,
                    LocationHeader: existing.LocationHeader);
            }

            // 3. Pending check: lease still valid
            if (string.Equals(existing.Status, "Pending", StringComparison.OrdinalIgnoreCase) && existing.ExpiresAtUtc > now)
            {
                _logger.LogInformation("Operation in progress for user {UserId}, op {Op}, key {Key}.", cleanUser, cleanOp, cleanKey);
                return new IdempotencyReplayResult(
                    IsReplay: true,
                    StatusCode: 409,
                    ResponsePayloadJson: null,
                    ErrorMessage: "OPERATION_IN_PROGRESS: A request with this operation key is currently processing. Please retry shortly.");
            }

            // 4. Pending lease expired -> recover by renewing lease
            existing.RenewPendingLease(cleanFingerprint, now, now.Add(PendingLeaseDuration));
            await _db.SaveChangesAsync(ct);
            return null;
        }

        // 5. Create new Pending record
        var newRecord = new IdempotencyRecordEntity(
            userId: cleanUser,
            operationType: cleanOp,
            resourceScope: cleanScope,
            operationKey: cleanKey,
            requestFingerprint: cleanFingerprint,
            createdAtUtc: now,
            expiresAtUtc: now.Add(PendingLeaseDuration));

        try
        {
            _db.IdempotencyRecords.Add(newRecord);
            await _db.SaveChangesAsync(ct);
            return null; // Proceed
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Race condition detected on idempotency record insert for user {UserId}, op {Op}, key {Key}.", cleanUser, cleanOp, cleanKey);
            _db.ChangeTracker.Clear();

            // Re-fetch and evaluate
            var reloaded = await _db.IdempotencyRecords
                .FirstOrDefaultAsync(r =>
                    r.UserId == cleanUser &&
                    r.OperationType == cleanOp &&
                    r.ResourceScope == cleanScope &&
                    r.OperationKey == cleanKey,
                    ct);

            if (reloaded != null)
            {
                if (!string.Equals(reloaded.RequestFingerprint, cleanFingerprint, StringComparison.OrdinalIgnoreCase))
                {
                    return new IdempotencyReplayResult(
                        IsReplay: true,
                        StatusCode: 409,
                        ResponsePayloadJson: null,
                        ErrorMessage: "IDEMPOTENCY_KEY_PAYLOAD_MISMATCH: Operation key was previously used with a different request payload.");
                }

                if (string.Equals(reloaded.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                {
                    return new IdempotencyReplayResult(
                        IsReplay: true,
                        StatusCode: reloaded.ResponseStatusCode,
                        ResponsePayloadJson: reloaded.ResponsePayloadJson,
                        LocationHeader: reloaded.LocationHeader);
                }

                return new IdempotencyReplayResult(
                    IsReplay: true,
                    StatusCode: 409,
                    ResponsePayloadJson: null,
                    ErrorMessage: "OPERATION_IN_PROGRESS: A request with this operation key is currently processing. Please retry shortly.");
            }

            throw;
        }
    }

    public async Task CompleteOperationAsync(
        string userId,
        string operationType,
        string resourceScope,
        string operationKey,
        int statusCode,
        object? responsePayload,
        string? locationHeader = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(operationKey))
        {
            return;
        }

        string cleanUser = userId?.Trim() ?? string.Empty;
        string cleanOp = operationType?.Trim() ?? string.Empty;
        string cleanScope = resourceScope?.Trim() ?? string.Empty;
        string cleanKey = operationKey.Trim();

        var existing = await _db.IdempotencyRecords
            .FirstOrDefaultAsync(r =>
                r.UserId == cleanUser &&
                r.OperationType == cleanOp &&
                r.ResourceScope == cleanScope &&
                r.OperationKey == cleanKey,
                ct);

        if (existing == null)
        {
            var now = _clock.GetUtcNow();
            existing = new IdempotencyRecordEntity(
                userId: cleanUser,
                operationType: cleanOp,
                resourceScope: cleanScope,
                operationKey: cleanKey,
                requestFingerprint: string.Empty,
                createdAtUtc: now,
                expiresAtUtc: now.Add(DefaultTtl));
            _db.IdempotencyRecords.Add(existing);
        }

        string? payloadJson = null;
        if (responsePayload != null)
        {
            payloadJson = responsePayload is string s ? s : JsonSerializer.Serialize(responsePayload);
        }

        existing.MarkCompleted(statusCode, payloadJson, locationHeader, _clock.GetUtcNow());
        await _db.SaveChangesAsync(ct);
    }

    public async Task FailOperationAsync(
        string userId,
        string operationType,
        string resourceScope,
        string operationKey,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(operationKey))
        {
            return;
        }

        string cleanUser = userId?.Trim() ?? string.Empty;
        string cleanOp = operationType?.Trim() ?? string.Empty;
        string cleanScope = resourceScope?.Trim() ?? string.Empty;
        string cleanKey = operationKey.Trim();

        var existing = await _db.IdempotencyRecords
            .FirstOrDefaultAsync(r =>
                r.UserId == cleanUser &&
                r.OperationType == cleanOp &&
                r.ResourceScope == cleanScope &&
                r.OperationKey == cleanKey,
                ct);

        if (existing != null && string.Equals(existing.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        {
            _db.IdempotencyRecords.Remove(existing);
            await _db.SaveChangesAsync(ct);
        }
    }
}
