namespace EnglishLiveTutor.Application.Features.Pronunciation.Contracts;

public sealed record AudioRecordingInfo(
    Guid Id,
    string UserId,
    string StorageKey,
    string ContentType,
    long FileSizeBytes,
    int DurationMilliseconds,
    bool IsDeleted);

public interface IAudioRecordingStore
{
    Task<AudioRecordingInfo?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns the recording only when it belongs to the authenticated learner.
    /// <paramref name="userId"/> must come from <c>ICurrentUser.UserId</c>.
    /// </summary>
    Task<AudioRecordingInfo?> GetOwnedAsync(
        Guid id,
        string userId,
        CancellationToken ct = default);

    Task<AudioRecordingInfo> AddAsync(
        string userId,
        string storageKey,
        string contentType,
        long fileSizeBytes,
        int durationMilliseconds,
        DateTimeOffset? expiresAt = null,
        string deletionStatus = "Retained",
        Guid? id = null,
        CancellationToken ct = default);
}
