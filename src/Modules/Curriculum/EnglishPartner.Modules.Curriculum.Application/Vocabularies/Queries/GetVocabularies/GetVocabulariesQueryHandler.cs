using EnglishPartner.Modules.Curriculum.Domain.Vocabularies.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Curriculum.Application.Vocabularies.Queries.GetVocabularies;

internal sealed class GetVocabulariesQueryHandler(
    IVocabularyRepository vocabularyRepository) : IQueryHandler<GetVocabulariesQuery, VocabulariesResponse>
{
    public async Task<Result<VocabulariesResponse>> Handle(GetVocabulariesQuery request, CancellationToken cancellationToken)
    {
        var items = await vocabularyRepository.GetByLanguageCodeAsync(
            request.LanguageCode, request.Page, request.PageSize, cancellationToken);

        var totalCount = await vocabularyRepository.CountByLanguageCodeAsync(
            request.LanguageCode, cancellationToken);

        var dtos = items.Select(v => new VocabularyListItem(
            v.Id, v.Word, v.Definition, v.PartOfSpeech, v.Level)).ToList();

        return new VocabulariesResponse(dtos, totalCount, request.Page, request.PageSize);
    }
}
