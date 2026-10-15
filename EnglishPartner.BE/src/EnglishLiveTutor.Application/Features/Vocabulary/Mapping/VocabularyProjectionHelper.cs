using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.Learning.Contracts;
using EnglishLiveTutor.Application.Features.Vocabulary.Contracts;
using EnglishLiveTutor.Domain.Learning.UserVocabularyProgress;
using EnglishLiveTutor.Domain.Vocabulary.Vocabularies;
using EnglishLiveTutor.Domain.Vocabulary.Vocabularies.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Mapping;

public static class VocabularyProjectionHelper
{
    public static async Task<VocabularyFlashcardDto?> GetFlashcardByVocabularyIdAsync(
        IAppReadDbContext db,
        Guid vocabularyId,
        Guid? senseId,
        string userId = "default-learner",
        CancellationToken ct = default)
    {
        var vocab = await db.Vocabularies
            .AsNoTracking()
            .Include(v => v.Pronunciations)
            .Include(v => v.WordFamilies)
            .Include(v => v.Senses).ThenInclude(s => s.Relations)
            .Include(v => v.Senses).ThenInclude(s => s.Collocations)
            .Include(v => v.Senses).ThenInclude(s => s.Examples)
            .Include(v => v.Senses).ThenInclude(s => s.GrammarPatterns)
            .FirstOrDefaultAsync(v => v.Id == vocabularyId && !v.IsDeleted, ct);

        if (vocab is null) return null;

        var activeSenses = vocab.Senses.Where(s => !s.IsDeleted).ToList();
        var sense = senseId.HasValue
            ? activeSenses.FirstOrDefault(s => s.Id == senseId.Value) ?? activeSenses.FirstOrDefault()
            : activeSenses.OrderByDescending(s => s.IsPrimary).ThenBy(s => s.DisplayOrder).FirstOrDefault();

        if (sense is null) return null;

        var progress = await db.UserVocabularyProgresses
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.SenseId == sense.Id, ct);

        var latestScore = await db.PronunciationAssessments
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.SenseId == sense.Id && a.OverallScore != null)
            .OrderByDescending(a => a.CreatedAtUtc)
            .Select(a => a.OverallScore)
            .FirstOrDefaultAsync(ct);

        return MapToFlashcardDto(Guid.Empty, vocab, sense, "New", "Pending", progress, latestScore);
    }

    public static async Task<List<VocabularyFlashcardDto>> GetFlashcardsBySensePairsAsync(
        IAppReadDbContext db,
        IReadOnlyList<(Guid VocabularyId, Guid SenseId)> pairs,
        string userId = "default-learner",
        CancellationToken ct = default)
    {
        if (pairs.Count == 0)
        {
            return new List<VocabularyFlashcardDto>();
        }

        var vocabIds = pairs.Select(p => p.VocabularyId).Distinct().ToList();
        var senseIds = pairs.Select(p => p.SenseId).Distinct().ToList();

        var vocabs = await db.Vocabularies
            .AsNoTracking()
            .Include(v => v.Pronunciations)
            .Include(v => v.WordFamilies)
            .Include(v => v.Senses).ThenInclude(s => s.Relations)
            .Include(v => v.Senses).ThenInclude(s => s.Collocations)
            .Include(v => v.Senses).ThenInclude(s => s.Examples)
            .Include(v => v.Senses).ThenInclude(s => s.GrammarPatterns)
            .Where(v => vocabIds.Contains(v.Id) && !v.IsDeleted)
            .ToDictionaryAsync(v => v.Id, ct);

        var progresses = await db.UserVocabularyProgresses
            .AsNoTracking()
            .Where(p => p.UserId == userId && senseIds.Contains(p.SenseId))
            .ToDictionaryAsync(p => p.SenseId, ct);

        var latestScores = await db.PronunciationAssessments
            .AsNoTracking()
            .Where(a => a.UserId == userId && senseIds.Contains(a.SenseId) && a.OverallScore != null)
            .GroupBy(a => a.SenseId)
            .Select(g => new
            {
                SenseId = g.Key,
                Score = g.OrderByDescending(x => x.CreatedAtUtc).Select(x => x.OverallScore).FirstOrDefault()
            })
            .ToDictionaryAsync(x => x.SenseId, x => x.Score, ct);

        var results = new List<VocabularyFlashcardDto>(pairs.Count);
        foreach (var (vId, sId) in pairs)
        {
            if (!vocabs.TryGetValue(vId, out var vocab))
                continue;

            var activeSenses = vocab.Senses.Where(s => !s.IsDeleted).ToList();
            var sense = activeSenses.FirstOrDefault(s => s.Id == sId) ?? activeSenses.FirstOrDefault();
            if (sense is null)
                continue;

            progresses.TryGetValue(sense.Id, out var prog);
            latestScores.TryGetValue(sense.Id, out var latestScore);
            results.Add(MapToFlashcardDto(Guid.Empty, vocab, sense, "New", "Pending", prog, latestScore));
        }

        return results;
    }

    public static async Task<DailyVocabularyPlanResponseDto> BuildDailyPlanResponseAsync(
        IAppReadDbContext db,
        Guid planId,
        string userId,
        CancellationToken ct)
    {
        var plan = await db.DailyVocabularyPlans
            .AsNoTracking()
            .Include(p => p.Items)
            .FirstAsync(p => p.Id == planId, ct);

        var orderedItems = plan.Items
            .Where(i => !i.IsDeleted)
            .OrderBy(i => i.DisplayOrder)
            .ToList();
        var vocabIds = orderedItems.Select(i => i.VocabularyId).Distinct().ToList();
        var senseIds = orderedItems.Select(i => i.SenseId).Distinct().ToList();

        var vocabs = await db.Vocabularies
            .AsNoTracking()
            .Include(v => v.Pronunciations)
            .Include(v => v.WordFamilies)
            .Include(v => v.Senses).ThenInclude(s => s.Relations)
            .Include(v => v.Senses).ThenInclude(s => s.Collocations)
            .Include(v => v.Senses).ThenInclude(s => s.Examples)
            .Include(v => v.Senses).ThenInclude(s => s.GrammarPatterns)
            .Where(v => vocabIds.Contains(v.Id) && !v.IsDeleted)
            .ToDictionaryAsync(v => v.Id, ct);

        var progresses = await db.UserVocabularyProgresses
            .AsNoTracking()
            .Where(p => p.UserId == userId && senseIds.Contains(p.SenseId))
            .ToDictionaryAsync(p => p.SenseId, ct);

        var latestScores = await db.PronunciationAssessments
            .AsNoTracking()
            .Where(a => a.UserId == userId && senseIds.Contains(a.SenseId) && a.OverallScore != null)
            .GroupBy(a => a.SenseId)
            .Select(g => new
            {
                SenseId = g.Key,
                Score = g.OrderByDescending(x => x.CreatedAtUtc).Select(x => x.OverallScore).FirstOrDefault()
            })
            .ToDictionaryAsync(x => x.SenseId, x => x.Score, ct);

        var flashcards = new List<VocabularyFlashcardDto>();
        foreach (var item in orderedItems)
        {
            if (!vocabs.TryGetValue(item.VocabularyId, out var vocab)) continue;
            var activeSenses = vocab.Senses.Where(s => !s.IsDeleted).ToList();
            var sense = activeSenses.FirstOrDefault(s => s.Id == item.SenseId) ?? activeSenses.FirstOrDefault();
            if (sense is null) continue;

            progresses.TryGetValue(sense.Id, out var prog);
            latestScores.TryGetValue(sense.Id, out var latestScore);

            flashcards.Add(MapToFlashcardDto(item.Id, vocab, sense, item.ItemType, item.Status, prog, latestScore));
        }

        return new DailyVocabularyPlanResponseDto(
            PlanId: plan.Id,
            UserId: plan.UserId,
            LearningDate: plan.LearningDate,
            TimeZoneId: plan.TimeZoneId,
            NewVocabularyTarget: plan.NewVocabularyTarget,
            TotalAssignedCount: orderedItems.Count,
            NewCount: orderedItems.Count(i => i.ItemType == "New"),
            PendingReviewCount: orderedItems.Count(i => i.ItemType == "Review"),
            CarryOverCount: orderedItems.Count(i => i.ItemType == "CarryOver"),
            CompletedCount: orderedItems.Count(i => i.Status == "Completed"),
            Status: plan.Status,
            Flashcards: flashcards
        );
    }

    public static VocabularyFlashcardDto MapToFlashcardDto(
        Guid dailyItemId,
        VocabularyEntity vocab,
        VocabularySenseEntity sense,
        string itemType,
        string itemStatus,
        UserVocabularyProgressEntity? prog,
        decimal? latestScore,
        DateTimeOffset? nowUtc = null)
    {
        var now = nowUtc ?? DateTimeOffset.UtcNow;
        bool isDue = prog?.NextReviewAt != null && prog.NextReviewAt <= now;

        return new VocabularyFlashcardDto(
            DailyItemId: dailyItemId,
            VocabularyId: vocab.Id,
            SenseId: sense.Id,
            Word: vocab.Word,
            PartOfSpeech: sense.PartOfSpeech,
            CefrLevel: sense.CefrLevel,
            DefinitionEn: sense.DefinitionEn,
            MeaningVi: sense.MeaningVi,
            DescriptionEn: sense.DescriptionEn,
            Pronunciations: vocab.Pronunciations
                .Where(p => !p.IsDeleted)
                .Select(p => new PronunciationVariantDto(p.Id, p.Accent, p.Ipa, p.AudioUrl, p.AudioSource, p.SyllableCount, p.StressPattern))
                .ToList(),
            Synonyms: sense.Relations
                .Where(r => !r.IsDeleted && r.RelationType == "Synonym")
                .Select(r => r.RelatedWord)
                .ToList(),
            Antonyms: sense.Relations
                .Where(r => !r.IsDeleted && r.RelationType == "Antonym")
                .Select(r => r.RelatedWord)
                .ToList(),
            WordFamilies: vocab.WordFamilies
                .Where(wf => !wf.IsDeleted)
                .Select(wf => new WordFamilyItemDto(wf.Id, wf.RelatedWord, wf.PartOfSpeech, wf.RelatedVocabularyId))
                .ToList(),
            Collocations: sense.Collocations
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new CollocationItemDto(c.Id, c.PhraseEn, c.MeaningVi, c.DisplayOrder))
                .ToList(),
            Examples: sense.Examples
                .Where(e => !e.IsDeleted)
                .OrderBy(e => e.DisplayOrder)
                .Select(e => new ExampleSentenceDto(e.Id, e.SentenceEn, e.SentenceVi, e.AudioUrl, e.CefrLevel, e.DisplayOrder))
                .ToList(),
            GrammarPatterns: sense.GrammarPatterns
                .Where(g => !g.IsDeleted)
                .Select(g => new GrammarPatternDto(g.Id, g.Pattern, g.Explanation, g.Example))
                .ToList(),
            ItemType: itemType,
            ItemStatus: itemStatus,
            LearningStatus: prog?.LearningStatus ?? "New",
            MasteryLevel: prog?.MasteryLevel ?? 0,
            IsNew: prog is null || prog.ReviewCount == 0,
            IsDueForReview: isDue,
            LatestPronunciationScore: latestScore,
            NextReviewAt: prog?.NextReviewAt,
            IntervalDays: prog?.IntervalDays ?? 0,
            EaseFactor: prog?.EaseFactor ?? 2.50m,
            TopicCategory: vocab.TopicCategory,
            FrequencyRank: vocab.FrequencyRank
        );
    }
}
