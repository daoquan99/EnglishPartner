using EnglishPartner.Modules.Curriculum.Domain;
using EnglishPartner.Modules.Curriculum.Domain.Grammars.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Grammars.Queries.GetGrammarById;

internal sealed class GetGrammarByIdQueryHandler(
    IGrammarRepository grammarRepository) : IQueryHandler<GetGrammarByIdQuery, GrammarDetailResponse>
{
    public async Task<Result<GrammarDetailResponse>> Handle(GetGrammarByIdQuery request, CancellationToken cancellationToken)
    {
        var grammar = await grammarRepository.GetByIdAsync(request.Id, cancellationToken);
        if (grammar is null) return Result.Failure<GrammarDetailResponse>(CurriculumErrors.Grammar.NotFound(request.Id));

        return new GrammarDetailResponse(
            grammar.Id,
            grammar.Title,
            grammar.LanguageCode,
            grammar.Level,
            grammar.Explanation,
            grammar.Formula,
            grammar.TopicId,
            grammar.Examples.Select(e => new GrammarExampleDto(
                e.Id, e.Sentence, e.Translation, e.Note, e.SortOrder)).ToList());
    }
}
