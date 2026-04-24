using EnglishPartner.Modules.Curriculum.Domain.Vocabularies.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Vocabularies.Queries.LookupVocabularies;

internal sealed class LookupVocabulariesQueryHandler(
    IVocabularyRepository vocabularyRepository) : IQueryHandler<LookupVocabulariesQuery, LookupVocabulariesResponse>
{
    public async Task<Result<LookupVocabulariesResponse>> Handle(LookupVocabulariesQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await vocabularyRepository.GetPagedLookupAsync(
            request.LanguageCode,
            request.SearchTerm,
            request.Page,
            request.PageSize,
            cancellationToken);

        var dtos = items.Select(x => new VocabularyLookupItem(x.Id, x.Word)).ToList();
        return new LookupVocabulariesResponse(dtos, totalCount);
    }
}
