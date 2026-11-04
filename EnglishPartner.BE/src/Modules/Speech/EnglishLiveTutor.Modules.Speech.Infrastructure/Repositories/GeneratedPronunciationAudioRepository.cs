using EnglishLiveTutor.Application.Modules.Speech;
using EnglishLiveTutor.Domain.Pronunciation;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;
using EnglishLiveTutor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Infrastructure.Repositories;

public sealed class GeneratedPronunciationAudioRepository : IGeneratedPronunciationAudioRepository
{
    private readonly ISpeechDbContext _db;

    public GeneratedPronunciationAudioRepository(ISpeechDbContext db)
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
                !a.IsDeleted)
            // No AudioData.Length check: the entity constructor rejects empty audio, and EF Core 10
            // rewrites byte[].Length > 0 to Any(), which SQL Server cannot translate for varbinary.
            .OrderByDescending(a => a.UpdatedAtUtc ?? a.CreatedAtUtc)
            .ThenByDescending(a => a.CreatedAtUtc)
            .ThenByDescending(a => a.Id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(GeneratedPronunciationAudioEntity entity, CancellationToken ct = default)
    {
        await _db.GeneratedPronunciationAudios.AddAsync(entity);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
