using EnglishPartner.Modules.Curriculum.Domain.Vocabularies.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;
using EnglishPartner.SharedKernel.Application.Caching;

namespace EnglishPartner.Modules.Curriculum.Application.Vocabularies.Queries.GetVocabularies;

internal sealed class GetVocabulariesQueryHandler(
    IVocabularyRepository vocabularyRepository,
    ICacheService cache) : IQueryHandler<GetVocabulariesQuery, VocabulariesResponse>
{
    private static readonly TimeSpan CacheTtlDuration = CacheTtl.Medium;

    public async Task<Result<VocabulariesResponse>> Handle(GetVocabulariesQuery request, CancellationToken cancellationToken)
    {
        // Skip cache for search queries — results too dynamic to cache effectively
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var (searchItems, searchTotal) = await vocabularyRepository.GetPagedAsync(
                request.LanguageCode, request.Page, request.PageSize,
                request.SearchTerm, request.Level, request.TopicId, cancellationToken);

            return new VocabulariesResponse(
                searchItems.Select(v => new VocabularyListItem(v.Id, v.Word, v.Definition, v.PartOfSpeech, v.Level)).ToList(),
                searchTotal, request.Page, request.PageSize);
        }
        //await cache.RemoveByPrefixAsync(CacheKeys.Curriculum.VocabulariesPrefix(request.LanguageCode), cancellationToken);

        // Cache paginated list — key encodes all filter dimensions
        var cacheKey = CacheKeys.Curriculum.VocabulariesQuery(request.LanguageCode, request.Page, request.PageSize, request.Level, request.TopicId);

        var response = await cache.GetOrSetAsync(cacheKey, async () =>
        {
            var (items, total) = await vocabularyRepository.GetPagedAsync(
                request.LanguageCode, request.Page, request.PageSize,
                null, request.Level, request.TopicId, cancellationToken);

            var dtos = items.Select(v => new VocabularyListItem(v.Id, v.Word, v.Definition, v.PartOfSpeech, v.Level)).ToList();
            return new VocabulariesResponse(dtos, total, request.Page, request.PageSize);
        }, CacheTtlDuration, cancellationToken);

        return response!;
    }
}
