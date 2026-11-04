using System.Text.RegularExpressions;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.Learning.Services;
using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Application.Features.Vocabulary.Services;
using EnglishLiveTutor.Application.Modules;
using EnglishLiveTutor.Application.Modules.Vocabulary;
using EnglishLiveTutor.Domain.Learning;
using EnglishLiveTutor.Domain.Vocabulary;
using EnglishLiveTutor.Domain.Vocabulary.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnglishLiveTutor.Application.Features.Vocabulary;

public sealed record VocabularyGenerationContext(
    string UserId,
    string ModelCode,
    int RequestedCount,
    string CefrLevel,
    string Topic,
    bool AssignToTodayPlan,
    int ExamplesPerSense = 2,
    List<string>? TargetTerms = null,
    string? SourceSentence = null);

public sealed class VocabularyGenerationWriter
{
    private readonly IVocabularyDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly IModuleDbContextFactory<IVocabularyDbContext> _dbFactory;
    private readonly IDailyVocabularyPlanInitializer _planInitializer;
    private readonly IPronunciationScoreReader _scoreReader;
    private readonly ILogger<VocabularyGenerationWriter> _logger;

    public VocabularyGenerationWriter(
        IVocabularyDbContext db,
        IUnitOfWork uow,
        IModuleDbContextFactory<IVocabularyDbContext> dbFactory,
        IDailyVocabularyPlanInitializer planInitializer,
        IPronunciationScoreReader scoreReader,
        ILogger<VocabularyGenerationWriter> logger)
    {
        _db = db;
        _uow = uow;
        _dbFactory = dbFactory;
        _planInitializer = planInitializer;
        _scoreReader = scoreReader;
        _logger = logger;
    }

    public async Task<List<VocabularyFlashcardDto>> SaveAsync(
        VocabularyGenerationContext context,
        List<GeneratedVocabularyRawSense> generatedSenses,
        CancellationToken ct = default)
    {
        string userId = context.UserId;
        string cefr = context.CefrLevel;
        string topic = context.Topic;
        var audit = new VocabularyGenerationAuditEntity(
            userId,
            context.ModelCode,
            VocabularyGenerationDefaults.PromptVersion,
            context.RequestedCount);

        // Branch SourceSentence (L4): Cap items to RequestedCount and insert original sentence as first example if absent
        if (!string.IsNullOrWhiteSpace(context.SourceSentence))
        {
            if (generatedSenses.Count > context.RequestedCount)
            {
                generatedSenses = generatedSenses.Take(context.RequestedCount).ToList();
            }

            string normalizedSource = TargetedVocabularyHelper.NormalizeTerm(context.SourceSentence);
            for (int i = 0; i < generatedSenses.Count; i++)
            {
                var s = generatedSenses[i];
                bool hasSourceSentence = s.Examples.Any(e =>
                    string.Equals(
                        TargetedVocabularyHelper.NormalizeTerm(e.SentenceEn),
                        normalizedSource,
                        StringComparison.OrdinalIgnoreCase));

                if (!hasSourceSentence)
                {
                    var updatedExamples = new List<GeneratedExampleRaw>(s.Examples);
                    updatedExamples.Insert(0, new GeneratedExampleRaw(context.SourceSentence.Trim(), string.Empty));
                    generatedSenses[i] = s with { Examples = updatedExamples };
                }
            }
        }

        var resultSenseIds = new List<(Guid VocabId, Guid SenseId)>();
        var normalizedWords = generatedSenses
            .Select(s => VocabularyEntity.NormalizeWord(s.Word))
            .Distinct()
            .ToList();

        var existingVocabularies = await _db.Vocabularies
            .Include(v => v.Senses)
            .Where(v => v.LanguageCode == "en" && normalizedWords.Contains(v.NormalizedWord))
            .ToListAsync(ct);

        var vocabularies = existingVocabularies
            .GroupBy(v => v.NormalizedWord)
            .ToDictionary(g => g.Key, g => g.OrderBy(v => v.CreatedAtUtc).ThenBy(v => v.Id).First());

        HashSet<string>? allowedTerms = null;
        if (context.TargetTerms != null && context.TargetTerms.Count > 0)
        {
            allowedTerms = context.TargetTerms
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(TargetedVocabularyHelper.NormalizeTerm)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        var matchedTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int accepted = 0;
        int rejected = 0;
        int senseOrderCounter = 1;
        int maxExamples = Math.Clamp(context.ExamplesPerSense, 1, 5);

        foreach (var rawSense in generatedSenses)
        {
            string wordRaw = rawSense.Word;
            string normTerm = TargetedVocabularyHelper.NormalizeTerm(wordRaw);
            string normWord = VocabularyEntity.NormalizeWord(wordRaw);
            if (string.IsNullOrWhiteSpace(normWord) || normWord.Length < 2)
            {
                rejected++;
                continue;
            }

            // TargetTerms filter: discard items not matching any target term
            if (allowedTerms != null && !allowedTerms.Contains(normTerm))
            {
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

                _db.Vocabularies.Add(vocab);
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
            foreach (var ex in rawSense.Examples.Take(maxExamples))
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
                _db.VocabularySenses.Add(sense);
            }

            resultSenseIds.Add((vocab.Id, sense.Id));
            if (allowedTerms != null && allowedTerms.Contains(normTerm))
            {
                matchedTerms.Add(normTerm);
            }
            accepted++;
        }

        // L6: Đếm rejected đúng theo số term không có mục khớp (không trừ theo RequestedCount - accepted)
        if (context.TargetTerms != null && context.TargetTerms.Count > 0)
        {
            var cleanTerms = context.TargetTerms
                .Select(TargetedVocabularyHelper.NormalizeTerm)
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            rejected = cleanTerms.Count(t => !matchedTerms.Contains(t));
        }

        // L6: Có TargetTerms/SourceSentence mà sau lọc không còn mục nào -> ném InvalidOperationException
        bool isTargetedOrSource = (context.TargetTerms != null && context.TargetTerms.Count > 0) ||
                                  !string.IsNullOrWhiteSpace(context.SourceSentence);

        if (isTargetedOrSource && accepted == 0)
        {
            throw new InvalidOperationException("AI không trả về từ vựng khớp với chữ đã chọn. Vui lòng thử lại.");
        }

        DailyVocabularyPlanEntity? todayPlan = null;
        bool isNewPlan = false;

        // Assign to today's plan if requested
        if (context.AssignToTodayPlan && resultSenseIds.Count > 0)
        {
            var excludedVocabIds = resultSenseIds.Select(r => r.VocabId).Distinct();
            var excludedSenseIds = resultSenseIds.Select(r => r.SenseId).Distinct();

            (todayPlan, isNewPlan) = await _planInitializer.PrepareTodayPlanAsync(
                userId: userId,
                reservedQuota: resultSenseIds.Count,
                excludedVocabularyIds: excludedVocabIds,
                excludedSenseIds: excludedSenseIds,
                ct: ct);

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
                        _db.DailyVocabularyItems.Add(addedItem);
                        availableQuota--;
                    }
                }
            }
        }

        audit.MarkCompleted(accepted, rejected);
        _db.VocabularyGenerationAudits.Add(audit);

        try
        {
            await _uow.CommitChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (isNewPlan && todayPlan is not null && DbUpdateExceptionClassifier.IsUniqueConstraintViolation(ex))
        {
            _logger.LogInformation(ex, "Trùng khóa khởi tạo DailyVocabularyPlan cho user {UserId}. Đang nạp lại plan đã tồn tại và gán từ vựng.", userId);

            // 1. Detach newly created plan and its staged items
            if (_db is DbContext efDb)
            {
                efDb.Entry(todayPlan).State = EntityState.Detached;
                foreach (var item in todayPlan.Items)
                {
                    efDb.Entry(item).State = EntityState.Detached;
                }

                var stagedSettings = efDb.ChangeTracker.Entries<UserLearningSettingsEntity>()
                    .FirstOrDefault(e => e.Entity.UserId == userId);
                if (stagedSettings is not null)
                {
                    var existingSettings = await _db.UserLearningSettings.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId, ct);
                    if (existingSettings is not null)
                    {
                        stagedSettings.State = EntityState.Detached;
                    }
                }
            }

            // 2. Reload the winner plan created concurrently
            var winnerPlan = await _db.DailyVocabularyPlans
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.UserId == userId && p.LearningDate == todayPlan.LearningDate, ct);

            if (winnerPlan is not null)
            {
                // 3. Assign to winner plan according to available quota
                int firstAssigned = winnerPlan.Items.Count(i => i.ItemType is "New" or "CarryOver");
                int quota = Math.Max(0, winnerPlan.NewVocabularyTarget - firstAssigned);

                if (quota > 0)
                {
                    int nextOrder = winnerPlan.Items.Count == 0 ? 1 : winnerPlan.Items.Max(i => i.DisplayOrder) + 1;
                    foreach (var (vId, sId) in resultSenseIds)
                    {
                        if (quota <= 0) break;

                        if (!winnerPlan.Items.Any(i => i.VocabularyId == vId || i.SenseId == sId))
                        {
                            var addedItem = winnerPlan.AddItem(vId, sId, "New", nextOrder++, DateTimeOffset.UtcNow);
                            _db.DailyVocabularyItems.Add(addedItem);
                            quota--;
                        }
                    }
                }

                await _uow.CommitChangesAsync(ct);
            }
            else
            {
                throw;
            }
        }

        var resultDtos = new List<VocabularyFlashcardDto>();
        foreach (var (vId, sId) in resultSenseIds)
        {
            var card = await VocabularyProjectionHelper.GetFlashcardByVocabularyIdAsync(
                _db,
                _scoreReader,
                vId,
                sId,
                userId,
                ct);
            if (card is not null) resultDtos.Add(card);
        }

        return resultDtos;
    }

    public async Task RecordFailureAsync(VocabularyGenerationContext context, Exception error)
    {
        try
        {
            await using var isolatedDb = await _dbFactory.CreateDbContextAsync(CancellationToken.None);
            var audit = new VocabularyGenerationAuditEntity(
                context.UserId,
                context.ModelCode,
                VocabularyGenerationDefaults.PromptVersion,
                context.RequestedCount);

            audit.MarkFailed(SanitizeErrorMessage(error));
            isolatedDb.VocabularyGenerationAudits.Add(audit);
            await isolatedDb.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception auditError)
        {
            _logger.LogWarning(auditError, "Unable to save the failed vocabulary generation audit.");
        }
    }

    public static string SanitizeErrorMessage(Exception? error)
    {
        if (error is null) return "Unknown error";

        string exType = error.GetType().Name;
        string rawMsg = error.Message ?? string.Empty;

        string cleaned = Regex.Replace(
            rawMsg,
            @"(?i)(?<=[?&]|\b)(?:api[-_]?key|key)(?:=|%3D)[^&\s]+",
            "key=[REDACTED]");
        cleaned = Regex.Replace(cleaned, @"(?i)x-goog-api-key\s*:\s*\S+", "x-goog-api-key: [REDACTED]");
        cleaned = Regex.Replace(cleaned, @"(?i)""(?:api[-_]?key|key)""\s*:\s*""[^""]*""", "\"key\":\"[REDACTED]\"");
        cleaned = Regex.Replace(cleaned, @"(?i)\bBearer\s+[A-Za-z0-9\-._~+/]+=*", "Bearer [REDACTED]");
        cleaned = Regex.Replace(cleaned, @"AIza[0-9A-Za-z_\-]{30,}", "[REDACTED]");

        string combined = $"{exType}: {cleaned}";
        if (combined.Length > 500)
        {
            combined = combined[..500];
        }

        return combined;
    }
}
