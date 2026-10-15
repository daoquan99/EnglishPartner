using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Vocabulary;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Queries.ListVocabularyFlashcards;

public sealed class ListVocabularyFlashcardsQueryHandler(IAppDbContextFactory dbFactory) : IQueryHandler<ListVocabularyFlashcardsQuery, PagedResultDto<VocabularyFlashcardDto>>
{
    public async Task<PagedResultDto<VocabularyFlashcardDto>> HandleAsync(ListVocabularyFlashcardsQuery request, CancellationToken ct = default)
    {
        var search = request.Search;
        var cefrLevel = request.CefrLevel;
        var topicCategory = request.TopicCategory;
        var page = request.Page;
        var pageSize = request.PageSize;

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var joinedQuery =
            from s in db.VocabularySenses.AsNoTracking()
            join v in db.Vocabularies.AsNoTracking() on s.VocabularyId equals v.Id
            select new { Vocab = v, Sense = s };

        if (!string.IsNullOrWhiteSpace(search))
        {
            string term = search.Trim().ToLowerInvariant();
            joinedQuery = joinedQuery.Where(x =>
                x.Vocab.NormalizedWord.Contains(term) ||
                (x.Vocab.TopicCategory != null && x.Vocab.TopicCategory.ToLower().Contains(term)) ||
                x.Sense.MeaningVi.ToLower().Contains(term) ||
                x.Sense.DefinitionEn.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(topicCategory))
        {
            string top = topicCategory.Trim();
            joinedQuery = joinedQuery.Where(x => x.Vocab.TopicCategory == top);
        }

        if (!string.IsNullOrWhiteSpace(cefrLevel))
        {
            string normCefr = cefrLevel.Trim().ToUpperInvariant();
            joinedQuery = joinedQuery.Where(x => x.Sense.CefrLevel == normCefr);
        }

        int totalCount = await joinedQuery.CountAsync(ct);

        var orderedPairs = joinedQuery
            .OrderByDescending(x => x.Vocab.CreatedAtUtc)
            .ThenBy(x => x.Sense.DisplayOrder)
            .ThenBy(x => x.Vocab.Id)
            .ThenBy(x => x.Sense.Id)
            .Select(x => new { VocabularyId = x.Vocab.Id, SenseId = x.Sense.Id });

        bool hasPaging = page.HasValue || pageSize.HasValue;
        int effPage = Math.Max(1, page ?? 1);
        int effSize = hasPaging
            ? Math.Clamp(pageSize ?? 20, 1, 500)
            : Math.Max(1, totalCount);

        var pagePairs = hasPaging
            ? await orderedPairs.Skip((effPage - 1) * effSize).Take(effSize).ToListAsync(ct)
            : await orderedPairs.ToListAsync(ct);

        var pageVocabIds = pagePairs.Select(p => p.VocabularyId).Distinct().ToList();
        var vocabDict = await db.Vocabularies
            .AsNoTracking()
            .Include(v => v.Pronunciations)
            .Include(v => v.WordFamilies)
            .Include(v => v.Senses).ThenInclude(s => s.Relations)
            .Include(v => v.Senses).ThenInclude(s => s.Collocations)
            .Include(v => v.Senses).ThenInclude(s => s.Examples)
            .Include(v => v.Senses).ThenInclude(s => s.GrammarPatterns)
            .Where(v => pageVocabIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, ct);

        var cards = new List<VocabularyFlashcardDto>(pagePairs.Count);
        foreach (var pair in pagePairs)
        {
            if (!vocabDict.TryGetValue(pair.VocabularyId, out var vocab))
                continue;
            var sense = vocab.Senses.FirstOrDefault(s => s.Id == pair.SenseId);
            if (sense is null)
                continue;

            cards.Add(VocabularyProjectionHelper.MapToFlashcardDto(
                Guid.Empty,
                vocab,
                sense,
                "New",
                "Pending",
                null,
                null));
        }

        var pagedResult = PagedResultDto<VocabularyFlashcardDto>.FromPage(cards, totalCount, page, pageSize);
        return pagedResult;
    }
}
