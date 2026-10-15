namespace EnglishLiveTutor.Domain.Prompts.Repositories;

public interface ISpeechSpeedRepository
{
    Task<SpeechSpeedEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<SpeechSpeedEntity?> GetByCodeIncludingDeletedAsync(string code, CancellationToken ct = default);
    Task<List<SpeechSpeedEntity>> ListDefaultsAsync(CancellationToken ct = default);
    Task AddAsync(SpeechSpeedEntity entity, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
