using EnglishPartner.Modules.Curriculum.Domain.ExampleSentences.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Interfaces;
using EnglishPartner.SharedKernel.Results;
using EnglishPartner.SharedKernel.Application.Caching;

namespace EnglishPartner.Modules.Curriculum.Application.ExampleSentences.Queries.GetExampleSentences;

internal sealed class GetExampleSentencesQueryHandler(
    IExampleSentenceRepository repository,
    ICacheService cache) : IQueryHandler<GetExampleSentencesQuery, ExampleSentencesResponse>
{
    private static readonly TimeSpan CacheTtlDuration = CacheTtl.Medium;

    public async Task<Result<ExampleSentencesResponse>> Handle(GetExampleSentencesQuery request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var (searchItems, searchTotal) = await repository.GetPagedAsync(
                languageCode: request.LanguageCode,
                page: request.Page,
                pageSize: request.PageSize,
                searchTerm: request.SearchTerm,
                vocabularyId: request.VocabularyId,
                grammarId: request.GrammarId,
                topicId: request.TopicId,
                cancellationToken: cancellationToken);
            
            return MapResponse(searchItems, searchTotal, request.Page, request.PageSize);
        }

        var cacheKey = CacheKeys.Curriculum.ExampleSentencesQuery(
            request.LanguageCode, request.Page, request.PageSize, 
            request.VocabularyId, request.GrammarId, request.TopicId);

        var response = await cache.GetOrSetAsync(cacheKey, async () =>
        {
            var (items, totalCount) = await repository.GetPagedAsync(
                languageCode: request.LanguageCode,
                page: request.Page,
                pageSize: request.PageSize,
                searchTerm: null,
                vocabularyId: request.VocabularyId,
                grammarId: request.GrammarId,
                topicId: request.TopicId,
                cancellationToken: cancellationToken);
            return MapResponse(items, totalCount, request.Page, request.PageSize);
        }, CacheTtlDuration, cancellationToken);

        return response!;
    }

    private static ExampleSentencesResponse MapResponse(
        List<EnglishPartner.Modules.Curriculum.Domain.ExampleSentences.ExampleSentence> items, 
        int totalCount, int page, int pageSize)
    {
        var dtos = items.Select(s => new ExampleSentenceResponse(
            s.Id,
            s.Text,
            s.AudioUrl,
            s.Note,
            s.Level,
            s.Translations.Select(t => new ExampleSentenceTranslationDto(t.LanguageCode, t.TranslatedText)).ToList()
        )).ToList();

        return new ExampleSentencesResponse(dtos, totalCount, page, pageSize);
    }
}
