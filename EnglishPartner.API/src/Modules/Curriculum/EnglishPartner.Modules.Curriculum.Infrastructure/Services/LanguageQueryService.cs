using EnglishPartner.Modules.Curriculum.Contracts.Services;
using EnglishPartner.Modules.Curriculum.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnglishPartner.Modules.Curriculum.Infrastructure.Services;

internal sealed class LanguageQueryService(CurriculumDbContext dbContext) : ILanguageQueryService
{
    public async Task<List<LanguageDto>> GetSupportedLanguagesAsync(CancellationToken cancellationToken = default)
    {
        var languages = await dbContext.Languages
            .AsNoTracking()
            .OrderBy(l => l.SortOrder)
            .ThenBy(l => l.Name)
            .Select(l => new LanguageDto(l.Code, l.Name, l.NativeName, l.SortOrder))
            .ToListAsync(cancellationToken);

        return languages;
    }
}
