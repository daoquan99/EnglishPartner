using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Learning;
using EnglishLiveTutor.Domain.Vocabulary;
using EnglishLiveTutor.Domain.Vocabulary.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnglishLiveTutor.Application.Features.Vocabulary;

public sealed record VocabularyGenerationContext(string UserId, string ModelCode, int RequestedCount, string CefrLevel, string Topic, bool AssignToTodayPlan);

public sealed class VocabularyGenerationWriter(IAppDbContextFactory dbFactory, ILogger<VocabularyGenerationWriter> logger)
{
    public async Task<List<VocabularyFlashcardDto>> SaveAsync(VocabularyGenerationContext context,
        List<GeneratedVocabularyRawSense> generatedSenses, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        string userId = context.UserId;
        string cefr = context.CefrLevel;
        string topic = context.Topic;
        var audit = new VocabularyGenerationAuditEntity(userId, context.ModelCode,
            VocabularyGenerationDefaults.PromptVersion, context.RequestedCount);
        var resultSenseIds = new List<(Guid VocabId, Guid SenseId)>();
        var normalizedWords = generatedSenses.Select(s => VocabularyEntity.NormalizeWord(s.Word)).Distinct().ToList();
        var existingVocabularies = await db.Vocabularies.Include(v => v.Senses)
            .Where(v => v.LanguageCode == "en" && normalizedWords.Contains(v.NormalizedWord)).ToListAsync(ct);
        var vocabularies = existingVocabularies.GroupBy(v => v.NormalizedWord)
            .ToDictionary(g => g.Key, g => g.OrderBy(v => v.CreatedAtUtc).ThenBy(v => v.Id).First());

        int accepted = 0;
        int rejected = 0;
        int senseOrderCounter = 1;

        foreach (var rawSense in generatedSenses)
        {
            string wordRaw = rawSense.Word;
            string normWord = VocabularyEntity.NormalizeWord(wordRaw);
            if (string.IsNullOrWhiteSpace(normWord) || normWord.Length < 2)
            {
                rejected++;
                continue;
            }

            vocabularies.TryGetValue(normWord, out var existingVocab);

            VocabularyEntity vocab;
            if (existingVocab is null)
            {
                vocab = VocabularyEntity.Create(
                    word: wordRaw,
                    languageCode: "en",
                    sourceType: nameof(VocabularySourceType.AI),
                    sourceModelId: context.ModelCode,
                    topicCategory: topic,
                    isActive: true);

                if (!string.IsNullOrWhiteSpace(rawSense.IpaUs))
                {
                    vocab.AddPronunciation(
                        accent: "US",
                        ipa: rawSense.IpaUs,
                        audioSource: "BrowserTTS");
                }

                if (!string.IsNullOrWhiteSpace(rawSense.IpaUk))
                {
                    vocab.AddPronunciation(
                        accent: "UK",
                        ipa: rawSense.IpaUk,
                        audioSource: "BrowserTTS");
                }

                foreach (var wf in rawSense.WordFamilies)
                {
                    if (!string.IsNullOrWhiteSpace(wf.RelatedWord))
                    {
                        vocab.AddWordFamily(
                            relatedWord: wf.RelatedWord,
                            partOfSpeech: string.IsNullOrWhiteSpace(wf.PartOfSpeech) ? "other" : wf.PartOfSpeech);
                    }
                }

                db.Vocabularies.Add(vocab);
                vocabularies[normWord] = vocab;
            }
            else
            {
                vocab = existingVocab;
            }

            string partOfSpeech = string.IsNullOrWhiteSpace(rawSense.PartOfSpeech) ? "noun" : rawSense.PartOfSpeech.Trim().ToLowerInvariant();
            string level = string.IsNullOrWhiteSpace(rawSense.CefrLevel) ? cefr : rawSense.CefrLevel;
            var duplicateSense = vocab.Senses.FirstOrDefault(s =>
                s.PartOfSpeech == partOfSpeech && s.CefrLevel == level &&
                string.Equals(s.DefinitionEn, rawSense.DefinitionEn.Trim(), StringComparison.OrdinalIgnoreCase) &&
                string.Equals(s.MeaningVi, rawSense.MeaningVi.Trim(), StringComparison.OrdinalIgnoreCase));
            if (duplicateSense is not null)
            {
                if (!resultSenseIds.Contains((vocab.Id, duplicateSense.Id)))
                    resultSenseIds.Add((vocab.Id, duplicateSense.Id));
                rejected++;
                continue;
            }

            var sense = vocab.AddSense(
                partOfSpeech: string.IsNullOrWhiteSpace(rawSense.PartOfSpeech) ? "noun" : rawSense.PartOfSpeech,
                definitionEn: rawSense.DefinitionEn,
                meaningVi: rawSense.MeaningVi,
                cefrLevel: string.IsNullOrWhiteSpace(rawSense.CefrLevel) ? cefr : rawSense.CefrLevel,
                descriptionEn: rawSense.DescriptionEn,
                isPrimary: vocab.Senses.Count == 0,
                displayOrder: senseOrderCounter++);

            foreach (var syn in rawSense.Synonyms)
            {
                if (!string.IsNullOrWhiteSpace(syn))
                {
                    sense.AddRelation("Synonym", syn);
                }
            }

            foreach (var ant in rawSense.Antonyms)
            {
                if (!string.IsNullOrWhiteSpace(ant))
                {
                    sense.AddRelation("Antonym", ant);
                }
            }

            int colOrder = 1;
            foreach (var col in rawSense.Collocations)
            {
                if (!string.IsNullOrWhiteSpace(col.PhraseEn))
                {
                    sense.AddCollocation(col.PhraseEn, col.MeaningVi, colOrder++);
                }
            }

            int exOrder = 1;
            foreach (var ex in rawSense.Examples)
            {
                if (!string.IsNullOrWhiteSpace(ex.SentenceEn))
                {
                    sense.AddExample(ex.SentenceEn, ex.SentenceVi, cefr, exOrder++);
                }
            }

            foreach (var gp in rawSense.GrammarPatterns)
            {
                if (!string.IsNullOrWhiteSpace(gp.Pattern))
                {
                    sense.AddGrammarPattern(gp.Pattern, gp.Explanation, gp.Example);
                }
            }

            if (existingVocab is not null)
            {
                db.VocabularySenses.Add(sense);
            }

            resultSenseIds.Add((vocab.Id, sense.Id));
            accepted++;
        }

        // Optional: assign to today's plan if plan already exists and has available quota
        if (context.AssignToTodayPlan && resultSenseIds.Count > 0)
        {
            var user = await db.Users
                .AsNoTracking()
                .Where(u => u.Id.ToString() == userId || u.Username == userId)
                .Select(u => new { u.TimeZoneId })
                .FirstOrDefaultAsync(ct);

            string userTz = string.IsNullOrWhiteSpace(user?.TimeZoneId) ? "Asia/Ho_Chi_Minh" : user.TimeZoneId;
            string today = DailyVocabularyPlanEntity.ResolveLocalLearningDate(userTz);

            var todayPlan = await db.DailyVocabularyPlans
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.UserId == userId && p.LearningDate == today, ct);

            if (todayPlan is not null)
            {
                int firstLearningAssigned = todayPlan.Items.Count(i => i.ItemType is "New" or "CarryOver");
                int availableQuota = Math.Max(0, todayPlan.NewVocabularyTarget - firstLearningAssigned);

                if (availableQuota > 0)
                {
                    int nextOrder = todayPlan.Items.Count == 0 ? 1 : todayPlan.Items.Max(i => i.DisplayOrder) + 1;
                    foreach (var (vId, sId) in resultSenseIds)
                    {
                        if (availableQuota <= 0) break;

                        if (!todayPlan.Items.Any(i => i.VocabularyId == vId || i.SenseId == sId))
                        {
                            var addedItem = todayPlan.AddItem(vId, sId, "New", nextOrder++, DateTimeOffset.UtcNow);
                            db.DailyVocabularyItems.Add(addedItem);
                            availableQuota--;
                        }
                    }
                }
            }
        }

        audit.MarkCompleted(accepted, rejected);
        db.VocabularyGenerationAudits.Add(audit);
        // Vocabulary graphs, today's plan and the audit are committed together.
        await db.SaveChangesAsync(ct);

        var resultDtos = new List<VocabularyFlashcardDto>();
        foreach (var (vId, sId) in resultSenseIds)
        {
            var card = await VocabularyProjectionHelper.GetFlashcardByVocabularyIdAsync(db, vId, sId, userId, ct);
            if (card is not null) resultDtos.Add(card);
        }

        return resultDtos;
    }

    public async Task RecordFailureAsync(VocabularyGenerationContext context, Exception error)
    {
        try
        {
            // A separate context cannot accidentally save partially processed vocabulary entities.
            await using var db = await dbFactory.CreateDbContextAsync(CancellationToken.None);
            var audit = new VocabularyGenerationAuditEntity(context.UserId, context.ModelCode,
                VocabularyGenerationDefaults.PromptVersion, context.RequestedCount);
            audit.MarkFailed(error.Message);
            db.VocabularyGenerationAudits.Add(audit);
            await db.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception auditError)
        {
            logger.LogWarning(auditError, "Unable to save the failed vocabulary generation audit.");
        }
    }
}
