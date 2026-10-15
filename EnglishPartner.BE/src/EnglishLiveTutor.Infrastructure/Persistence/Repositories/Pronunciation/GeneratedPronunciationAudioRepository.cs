using EnglishLiveTutor.Domain.Pronunciation.GeneratedAudio;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Persistence.Repositories.Pronunciation;

public sealed class GeneratedPronunciationAudioRepository : IGeneratedPronunciationAudioRepository
{
    private readonly AppDbContext _db;

    public GeneratedPronunciationAudioRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<GeneratedPronunciationAudioEntity?> GetValidAudioAsync(
        string targetType,
        Guid targetId,
        string voiceName,
        decimal speechSpeed,
        string accent,
        string modelCode,
        string textHash,
        CancellationToken ct = default)
    {
        return await _db.GeneratedPronunciationAudios
            .AsNoTracking()
            .Where(a =>
                a.TargetType == targetType &&
                a.TargetId == targetId &&
                a.VoiceName == voiceName &&
                a.SpeechSpeed == speechSpeed &&
                a.Accent == accent &&
                a.ModelCode == modelCode &&
                a.TextHash == textHash &&
                a.IsValid &&
                !a.IsDeleted &&
                a.AudioData != null &&
                a.AudioData.Length > 0)
            .OrderByDescending(a => a.UpdatedAtUtc ?? a.CreatedAtUtc)
            .ThenByDescending(a => a.CreatedAtUtc)
            .ThenByDescending(a => a.Id)
            .FirstOrDefaultAsync(ct);
    }

    public Task AddAsync(GeneratedPronunciationAudioEntity entity, CancellationToken ct = default)
    {
        _db.GeneratedPronunciationAudios.Add(entity);
        return Task.CompletedTask;
    }
}
