using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Learning;
using EnglishLiveTutor.Domain.Vocabulary;
using EnglishLiveTutor.Domain.Vocabulary.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Vocabulary;

public static class VocabularyProjectionHelper
{
    public static async Task<VocabularyFlashcardDto?> GetFlashcardByVocabularyIdAsync(
        IAppDbContext db,
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
            .FirstOrDefaultAsync(v => v.Id == vocabularyId, ct);

        if (vocab is null) return null;
        var sense = senseId.HasValue
            ? vocab.Senses.FirstOrDefault(s => s.Id == senseId.Value) ?? vocab.Senses.FirstOrDefault()
            : vocab.Senses.OrderByDescending(s => s.IsPrimary).ThenBy(s => s.DisplayOrder).FirstOrDefault();

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

    public static async Task<DailyVocabularyPlanResponseDto> BuildDailyPlanResponseAsync(
        IAppDbContext db,
        Guid planId,
        string userId,
        CancellationToken ct)
    {
        var plan = await db.DailyVocabularyPlans
            .AsNoTracking()
            .Include(p => p.Items)
            .FirstAsync(p => p.Id == planId, ct);

        var orderedItems = plan.Items.OrderBy(i => i.DisplayOrder).ToList();
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
            .Where(v => vocabIds.Contains(v.Id))
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
            var sense = vocab.Senses.FirstOrDefault(s => s.Id == item.SenseId) ?? vocab.Senses.FirstOrDefault();
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
        decimal? latestScore)
    {
        var now = DateTimeOffset.UtcNow;
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
                .Select(p => new PronunciationVariantDto(p.Id, p.Accent, p.Ipa, p.AudioUrl, p.AudioSource, p.SyllableCount, p.StressPattern))
                .ToList(),
            Synonyms: sense.Relations
                .Where(r => r.RelationType == "Synonym")
                .Select(r => r.RelatedWord)
                .ToList(),
            Antonyms: sense.Relations
                .Where(r => r.RelationType == "Antonym")
                .Select(r => r.RelatedWord)
                .ToList(),
            WordFamilies: vocab.WordFamilies
                .Select(wf => new WordFamilyItemDto(wf.Id, wf.RelatedWord, wf.PartOfSpeech, wf.RelatedVocabularyId))
                .ToList(),
            Collocations: sense.Collocations
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new CollocationItemDto(c.Id, c.PhraseEn, c.MeaningVi, c.DisplayOrder))
                .ToList(),
            Examples: sense.Examples
                .OrderBy(e => e.DisplayOrder)
                .Select(e => new ExampleSentenceDto(e.Id, e.SentenceEn, e.SentenceVi, e.AudioUrl, e.CefrLevel, e.DisplayOrder))
                .ToList(),
            GrammarPatterns: sense.GrammarPatterns
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
            EaseFactor: prog?.EaseFactor ?? 2.50m
        );
    }
}
