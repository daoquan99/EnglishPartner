using EnglishPartner.Modules.Curriculum.Domain.Grammars.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Grammars.Queries.GetGrammars;

internal sealed class GetGrammarsQueryHandler(
    IGrammarRepository grammarRepository) : IQueryHandler<GetGrammarsQuery, GrammarsResponse>
{
    public async Task<Result<GrammarsResponse>> Handle(GetGrammarsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await grammarRepository.GetPagedLookupAsync(
            request.LanguageCode, request.SearchTerm, request.Page, request.PageSize, cancellationToken);

        var dtos = items.Select(g => new GrammarListItemResponse(
            g.Id, g.Title, g.Formula, g.Level, g.TopicId)).ToList();

        return new GrammarsResponse(dtos, totalCount, request.Page, request.PageSize);
    }
}
