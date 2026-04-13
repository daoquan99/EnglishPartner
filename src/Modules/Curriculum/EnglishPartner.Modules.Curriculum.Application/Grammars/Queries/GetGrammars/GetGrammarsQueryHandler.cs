using EnglishPartner.Modules.Curriculum.Domain.Grammars.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Grammars.Queries.GetGrammars;

internal sealed class GetGrammarsQueryHandler(
    IGrammarRepository grammarRepository) : IQueryHandler<GetGrammarsQuery, List<GrammarListItemResponse>>
{
    public async Task<Result<List<GrammarListItemResponse>>> Handle(GetGrammarsQuery request, CancellationToken cancellationToken)
    {
        var grammars = await grammarRepository.GetByLanguageCodeAsync(request.LanguageCode, cancellationToken);
        
        return grammars.Select(g => new GrammarListItemResponse(
            g.Id, g.Title, g.Level, g.TopicId)).ToList();
    }
}
