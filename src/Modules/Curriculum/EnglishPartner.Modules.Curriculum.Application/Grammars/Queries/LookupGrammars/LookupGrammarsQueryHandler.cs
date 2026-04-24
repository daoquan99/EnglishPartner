using EnglishPartner.Modules.Curriculum.Domain.Grammars.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Grammars.Queries.LookupGrammars;

internal sealed class LookupGrammarsQueryHandler(
    IGrammarRepository grammarRepository) : IQueryHandler<LookupGrammarsQuery, LookupGrammarsResponse>
{
    public async Task<Result<LookupGrammarsResponse>> Handle(LookupGrammarsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await grammarRepository.GetPagedLookupAsync(
            request.LanguageCode,
            request.SearchTerm,
            request.Page,
            request.PageSize,
            cancellationToken);

        var dtos = items.Select(x => new GrammarLookupItem(x.Id, x.Title)).ToList();
        return new LookupGrammarsResponse(dtos, totalCount);
    }
}
