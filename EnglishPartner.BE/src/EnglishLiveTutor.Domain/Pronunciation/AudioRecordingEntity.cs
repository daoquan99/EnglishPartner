using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Pronunciation;

public sealed class AudioRecordingEntity : AggregateRoot<Guid>
{
    public string UserId { get; private set; } = "default-learner";
    public string StorageKey { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = "audio/webm";
    public long FileSizeBytes { get; private set; }
    public int DurationMilliseconds { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public string DeletionStatus { get; private set; } = "Retained";

    private AudioRecordingEntity()
    {
    }

    public AudioRecordingEntity(
        string userId,
        string storageKey,
        string contentType,
        long fileSizeBytes,
        int durationMilliseconds,
        DateTimeOffset? expiresAt = null,
        string deletionStatus = "Retained",
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        UserId = string.IsNullOrWhiteSpace(userId) ? "default-learner" : userId.Trim();
        StorageKey = storageKey?.Trim() ?? string.Empty;
        ContentType = string.IsNullOrWhiteSpace(contentType) ? "audio/webm" : contentType.Trim();
        FileSizeBytes = fileSizeBytes;
        DurationMilliseconds = durationMilliseconds;
        ExpiresAt = expiresAt;
        DeletionStatus = string.IsNullOrWhiteSpace(deletionStatus) ? "Retained" : deletionStatus.Trim();
    }
}
