using EnglishPartner.Modules.Curriculum.Domain.Languages.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Languages.Queries.GetLanguages;

internal sealed class GetLanguagesQueryHandler(
    ILanguageRepository languageRepository) : IQueryHandler<GetLanguagesQuery, IReadOnlyList<LanguageResponse>>
{
    public async Task<Result<IReadOnlyList<LanguageResponse>>> Handle(
        GetLanguagesQuery request, 
        CancellationToken cancellationToken)
    {
        var languages = await languageRepository.GetAllAsync(cancellationToken);

        var response = languages
            .OrderBy(l => l.SortOrder)
            .ThenBy(l => l.Name)
            .Select(l => new LanguageResponse(
                l.Id,
                l.Code,
                l.Name,
                l.NativeName,
                l.SortOrder))
            .ToList();

        return response;
    }
}
