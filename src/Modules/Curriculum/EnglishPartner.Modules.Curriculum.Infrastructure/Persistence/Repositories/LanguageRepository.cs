using EnglishPartner.Modules.Curriculum.Domain.Languages;
using EnglishPartner.Modules.Curriculum.Domain.Languages.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Curriculum.Infrastructure.Persistence.Repositories;

internal sealed class LanguageRepository(CurriculumDbContext dbContext) : ILanguageRepository
{
    public async Task<Language?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => await dbContext.Languages.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Language?> GetByCodeAsync(string code, CancellationToken cancellationToken)
        => await dbContext.Languages.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

    public async Task<List<Language>> GetAllAsync(CancellationToken cancellationToken)
        => await dbContext.Languages.OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);

    public void Add(Language language) => dbContext.Languages.Add(language);
    public void Update(Language language) => dbContext.Languages.Update(language);
    public void Remove(Language language) => dbContext.Languages.Remove(language);
}
