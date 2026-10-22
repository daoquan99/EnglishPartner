namespace EnglishLiveTutor.Infrastructure.Persistence.Entities;

public sealed class IdempotencyRecordEntity
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string OperationType { get; private set; } = string.Empty;
    public string ResourceScope { get; private set; } = string.Empty;
    public string OperationKey { get; private set; } = string.Empty;
    public string RequestFingerprint { get; private set; } = string.Empty;
    public string Status { get; private set; } = "Pending"; // "Pending", "Completed", "Failed"
    public int ResponseStatusCode { get; private set; }
    public string? ResponsePayloadJson { get; private set; }
    public string? LocationHeader { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private IdempotencyRecordEntity()
    {
    }

    public IdempotencyRecordEntity(
        string userId,
        string operationType,
        string resourceScope,
        string operationKey,
        string requestFingerprint,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        UserId = userId?.Trim() ?? string.Empty;
        OperationType = operationType?.Trim() ?? string.Empty;
        ResourceScope = resourceScope?.Trim() ?? string.Empty;
        OperationKey = operationKey?.Trim() ?? string.Empty;
        RequestFingerprint = requestFingerprint?.Trim() ?? string.Empty;
        Status = "Pending";
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public void MarkCompleted(
        int statusCode,
        string? responsePayloadJson,
        string? locationHeader,
        DateTimeOffset completedAtUtc)
    {
        Status = "Completed";
        ResponseStatusCode = statusCode;
        ResponsePayloadJson = responsePayloadJson;
        LocationHeader = locationHeader;
        CompletedAtUtc = completedAtUtc;
    }

    public void MarkFailed(DateTimeOffset completedAtUtc)
    {
        Status = "Failed";
        CompletedAtUtc = completedAtUtc;
    }

    public void RenewPendingLease(string requestFingerprint, DateTimeOffset now, DateTimeOffset newExpiresAtUtc)
    {
        RequestFingerprint = requestFingerprint;
        Status = "Pending";
        CreatedAtUtc = now;
        ExpiresAtUtc = newExpiresAtUtc;
        CompletedAtUtc = null;
        ResponsePayloadJson = null;
        ResponseStatusCode = 0;
        LocationHeader = null;
    }
}
