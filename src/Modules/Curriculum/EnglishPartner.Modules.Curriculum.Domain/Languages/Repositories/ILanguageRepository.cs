namespace EnglishPartner.Modules.Curriculum.Domain.Languages.Repositories;

public interface ILanguageRepository
{
    Task<Language?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<List<Language>> GetAllAsync(CancellationToken cancellationToken = default);
    void Add(Language language);
}
