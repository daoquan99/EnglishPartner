namespace EnglishLiveTutor.Domain.Pronunciation.Repositories;

public interface IAudioRecordingRepository
{
    Task<AudioRecordingEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AudioRecordingEntity?> GetOwnedByIdAsync(
        Guid id,
        string userId,
        CancellationToken ct = default);
    void Add(AudioRecordingEntity recording);
}
