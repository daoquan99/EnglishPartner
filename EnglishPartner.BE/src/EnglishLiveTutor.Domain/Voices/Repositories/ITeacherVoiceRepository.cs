namespace EnglishLiveTutor.Domain.Voices.Repositories;

public interface ITeacherVoiceRepository
{
    Task<TeacherVoiceEntity?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<TeacherVoiceEntity>> ListVoicesAsync(bool onlyEnabled = true, bool onlyFeatured = false, string? gender = null, CancellationToken ct = default);
    Task AddAsync(TeacherVoiceEntity voice, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
