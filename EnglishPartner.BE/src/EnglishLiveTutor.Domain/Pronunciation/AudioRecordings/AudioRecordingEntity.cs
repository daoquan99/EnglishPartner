using EnglishLiveTutor.Domain.Common.Primitives;

namespace EnglishLiveTutor.Domain.Pronunciation.AudioRecordings;

/// <summary>
/// Represents metadata about a learner's pronunciation recording submission.
/// Currently operates in metadata-only mode (raw audio bytes are forwarded to the assessment provider
/// and not retained in blob storage).
/// </summary>
public sealed class AudioRecordingEntity : AggregateRoot<Guid>
{
    public const string MetadataOnlyStatus = "MetadataOnly";

    public string UserId { get; private set; } = "default-learner";
    public string StorageKey { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = "audio/webm";
    public long FileSizeBytes { get; private set; }
    public int DurationMilliseconds { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public string DeletionStatus { get; private set; } = MetadataOnlyStatus;

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
        string deletionStatus = MetadataOnlyStatus,
        Guid? id = null)
        : base(id ?? Guid.NewGuid())
    {
        UserId = string.IsNullOrWhiteSpace(userId) ? "default-learner" : userId.Trim();
        StorageKey = storageKey?.Trim() ?? string.Empty;
        ContentType = string.IsNullOrWhiteSpace(contentType) ? "audio/webm" : contentType.Trim();
        FileSizeBytes = Math.Max(0, fileSizeBytes);
        DurationMilliseconds = Math.Max(0, durationMilliseconds);
        ExpiresAt = expiresAt;
        DeletionStatus = string.IsNullOrWhiteSpace(deletionStatus) ? MetadataOnlyStatus : deletionStatus.Trim();
    }

    public static AudioRecordingEntity CreateMetadataOnly(
        string userId,
        string? contentType,
        long fileSizeBytes,
        int durationMilliseconds,
        Guid? id = null)
    {
        var recordingId = id ?? Guid.NewGuid();
        string cleanUser = string.IsNullOrWhiteSpace(userId) ? "default-learner" : userId.Trim();
        return new AudioRecordingEntity(
            userId: cleanUser,
            storageKey: $"metadata://recordings/{cleanUser}/{recordingId:N}",
            contentType: string.IsNullOrWhiteSpace(contentType) ? "audio/webm" : contentType.Trim(),
            fileSizeBytes: fileSizeBytes,
            durationMilliseconds: durationMilliseconds,
            expiresAt: null,
            deletionStatus: MetadataOnlyStatus,
            id: recordingId);
    }
}
