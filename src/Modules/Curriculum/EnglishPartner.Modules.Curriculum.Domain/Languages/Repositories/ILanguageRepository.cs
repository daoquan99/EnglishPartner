namespace EnglishPartner.Modules.Curriculum.Domain.Languages.Repositories;

public interface ILanguageRepository
{
    Task<Language?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Language?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<List<Language>> GetAllAsync(CancellationToken cancellationToken = default);
    void Add(Language language);
    void Update(Language language);
    void Remove(Language language);
}
