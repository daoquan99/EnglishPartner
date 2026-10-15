namespace EnglishLiveTutor.Domain.Voices;

public interface ITeacherVoiceRepository
{
    Task<TeacherVoiceEntity?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<TeacherVoiceEntity?> GetByIdIncludingDeletedAsync(string id, CancellationToken ct = default);
    Task AddAsync(TeacherVoiceEntity voice, CancellationToken ct = default);
}
