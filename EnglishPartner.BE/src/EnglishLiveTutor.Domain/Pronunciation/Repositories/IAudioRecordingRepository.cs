namespace EnglishLiveTutor.Domain.Pronunciation.Repositories;

public interface IAudioRecordingRepository
{
    Task<AudioRecordingEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    void Add(AudioRecordingEntity recording);
}
