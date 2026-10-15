namespace EnglishLiveTutor.Domain.Pronunciation.Repositories;

public interface IGeneratedPronunciationAudioRepository
{
    Task<GeneratedPronunciationAudioEntity?> GetValidAudioAsync(
        string targetType,
        Guid targetId,
        string voiceName,
        decimal speechSpeed,
        string accent,
        string modelCode,
        string textHash,
        CancellationToken ct = default);

    Task AddAsync(GeneratedPronunciationAudioEntity entity, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
