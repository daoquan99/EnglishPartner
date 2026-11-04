using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Services;

public sealed class AudioRecordingStore(
    IAudioRecordingRepository repository)
    : IAudioRecordingStore
{
    public async Task<AudioRecordingInfo?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var recording = await repository.GetByIdAsync(id, ct);
        return recording is null ? null : ToInfo(recording);
    }

    public async Task<AudioRecordingInfo?> GetOwnedAsync(
        Guid id,
        string userId,
        CancellationToken ct = default)
    {
        var recording = await repository.GetOwnedByIdAsync(id, userId, ct);
        return recording is null ? null : ToInfo(recording);
    }

    public Task<AudioRecordingInfo> AddAsync(
        string userId,
        string storageKey,
        string contentType,
        long fileSizeBytes,
        int durationMilliseconds,
        DateTimeOffset? expiresAt = null,
        string deletionStatus = "Retained",
        Guid? id = null,
        CancellationToken ct = default)
    {
        var recording = new AudioRecordingEntity(
            userId: userId,
            storageKey: storageKey,
            contentType: contentType,
            fileSizeBytes: fileSizeBytes,
            durationMilliseconds: durationMilliseconds,
            expiresAt: expiresAt,
            deletionStatus: deletionStatus,
            id: id);

        repository.Add(recording);
        return Task.FromResult(ToInfo(recording));
    }

    private static AudioRecordingInfo ToInfo(AudioRecordingEntity recording) =>
        new(
            Id: recording.Id,
            UserId: recording.UserId,
            StorageKey: recording.StorageKey,
            ContentType: recording.ContentType,
            FileSizeBytes: recording.FileSizeBytes,
            DurationMilliseconds: recording.DurationMilliseconds,
            IsDeleted: recording.IsDeleted);
}
