using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Application.Features.Vocabulary.Contracts;
using EnglishLiveTutor.Application.Features.Vocabulary.Mapping;
using EnglishLiveTutor.Application.Features.Vocabulary.Ports;
using EnglishLiveTutor.Domain.Common.ValueObjects;
using EnglishLiveTutor.Domain.Learning.DailyVocabularyPlans;
using EnglishLiveTutor.Domain.Vocabulary.GenerationRuns;
using EnglishLiveTutor.Domain.Vocabulary.Vocabularies;
using EnglishLiveTutor.Domain.Vocabulary.Vocabularies.Entities;
using EnglishLiveTutor.Domain.Vocabulary.Vocabularies.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Policies;

public sealed record VocabularyGenerationContext(
    string UserId,
    string ModelCode,
    int RequestedCount,
    string CefrLevel,
    string Topic,
    bool AssignToTodayPlan,
    string TimeZoneId = LearningDate.DefaultTimeZoneId
);

public sealed class VocabularyGenerationWriter
{
    private readonly IVocabularyRepository? _vocabRepo;
    private readonly IDailyVocabularyPlanRepository? _planRepo;
    private readonly IUnitOfWork? _unitOfWork;
    private readonly IApplicationReadDbContextFactory _readDbFactory;
    private readonly IVocabularyGenerationFailureAuditWriter? _failureAuditWriter;
    private readonly IAppDbContextFactory? _legacyDbFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<VocabularyGenerationWriter> _logger;

    [ActivatorUtilitiesConstructor]
    public VocabularyGenerationWriter(
        IVocabularyRepository vocabRepo,
        IDailyVocabularyPlanRepository planRepo,
        IUnitOfWork unitOfWork,
        IApplicationReadDbContextFactory readDbFactory,
        IVocabularyGenerationFailureAuditWriter failureAuditWriter,
        TimeProvider timeProvider,
        ILogger<VocabularyGenerationWriter> logger)
    {
        _vocabRepo = vocabRepo;
        _planRepo = planRepo;
        _unitOfWork = unitOfWork;
        _readDbFactory = readDbFactory;
        _failureAuditWriter = failureAuditWriter;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// Convenience constructor for standalone unit tests using IAppDbContextFactory.
    /// </summary>
    public VocabularyGenerationWriter(
        IAppDbContextFactory dbFactory,
        ILogger<VocabularyGenerationWriter> logger,
        TimeProvider? timeProvider = null)
    {
        _legacyDbFactory = dbFactory;
        _readDbFactory = dbFactory;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger;
    }

    public async Task<List<VocabularyFlashcardDto>> SaveAsync(
        VocabularyGenerationContext context,
        List<GeneratedVocabularyRawSense> generatedSenses,
        CancellationToken ct = default)
    {
        bool committed = false;
        var resultSenseIds = new List<(Guid VocabId, Guid SenseId)>();
        var inMemoryFallbacks = new List<(VocabularyEntity Vocab, VocabularySenseEntity Sense)>();

        try
        {
            if (_vocabRepo is not null && _planRepo is not null && _unitOfWork is not null)
            {
                await StageWithRepositoriesAsync(context, generatedSenses, resultSenseIds, inMemoryFallbacks, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                committed = true;
            }
            else if (_legacyDbFactory is not null)
            {
                await using var db = await _legacyDbFactory.CreateDbContextAsync(ct);
                await StageWithDbContextAsync(db, context, generatedSenses, resultSenseIds, inMemoryFallbacks, ct);
                await db.SaveChangesAsync(ct);
                committed = true;
            }
        }
        catch (Exception ex) when (!committed)
        {
            await RecordFailureAsync(context, ex);
            throw;
        }

        // Readback runs AFTER commit; a readback failure must never report that the write rolled back (Section 5).
        try
        {
            await using var readDb = await _readDbFactory.CreateReadContextAsync(ct);
            return await VocabularyProjectionHelper.GetFlashcardsBySenseIdsBatchAsync(
                readDb,
                resultSenseIds,
                context.UserId,
                ct);
        }
        catch (Exception readbackEx)
        {
            _logger.LogWarning(
                readbackEx,
                "Vocabulary generation committed successfully for user {UserId}, but post-commit flashcard projection failed. Returning in-memory mapped DTOs.",
                context.UserId);

            return inMemoryFallbacks
                .Select(x => VocabularyProjectionHelper.MapToFlashcardDto(
                    Guid.Empty,
                    x.Vocab,
                    x.Sense,
                    "New",
                    "Pending",
                    null,
                    null,
                    _timeProvider.GetUtcNow()))
                .ToList();
        }
    }

    private async Task StageWithRepositoriesAsync(
        VocabularyGenerationContext context,
        List<GeneratedVocabularyRawSense> generatedSenses,
        List<(Guid VocabId, Guid SenseId)> resultSenseIds,
        List<(VocabularyEntity Vocab, VocabularySenseEntity Sense)> inMemoryFallbacks,
        CancellationToken ct)
    {
        string userId = context.UserId;
        string cefr = context.CefrLevel;
        string topic = context.Topic;
        var now = _timeProvider.GetUtcNow();

        var audit = new VocabularyGenerationAuditEntity(
            userId,
            context.ModelCode,
            VocabularyGenerationDefaults.PromptVersion,
            context.RequestedCount);

        var normalizedWords = generatedSenses
            .Select(s => VocabularyEntity.NormalizeWord(s.Word))
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Distinct()
            .ToList();

        var existingVocabularies = await _vocabRepo!.GetByNormalizedWordsWithSensesAsync(normalizedWords, "en", ct);
        var vocabularies = existingVocabularies
            .GroupBy(v => v.NormalizedWord)
            .ToDictionary(g => g.Key, g => g.OrderBy(v => v.CreatedAtUtc).ThenBy(v => v.Id).First());

        var (accepted, rejected) = await PopulateVocabularyGraphAsync(
            context,
            generatedSenses,
            vocabularies,
            addVocabAsync: v => _vocabRepo.AddAsync(v, ct),
            addSenseAsync: s => _vocabRepo.AddSenseAsync(s, ct),
            resultSenseIds,
            inMemoryFallbacks);

        if (context.AssignToTodayPlan && resultSenseIds.Count > 0)
        {
            string today = LearningDate.ResolveLocalDateString(context.TimeZoneId, now);
            var todayPlan = await _planRepo!.GetByUserAndDateWithItemsAsync(userId, today, ct);

            if (todayPlan is null)
            {
                todayPlan = new DailyVocabularyPlanEntity(
                    userId,
                    today,
                    context.TimeZoneId,
                    newVocabularyTarget: context.RequestedCount,
                    currentUtcTime: now);
                await _planRepo.AddAsync(todayPlan, ct);
            }

            int nextOrder = todayPlan.Items.Count == 0 ? 1 : todayPlan.Items.Max(i => i.DisplayOrder) + 1;
            foreach (var (vId, sId) in resultSenseIds)
            {
                if (!todayPlan.Items.Any(i => i.SenseId == sId))
                {
                    var addedItem = todayPlan.AddItem(vId, sId, "New", nextOrder++, now);
                    await _planRepo.AddItemAsync(addedItem, ct);
                }
            }
        }

        audit.MarkCompleted(accepted, rejected);
        await _vocabRepo.AddGenerationAuditAsync(audit, ct);
    }

    private async Task StageWithDbContextAsync(
        IAppDbContext db,
        VocabularyGenerationContext context,
        List<GeneratedVocabularyRawSense> generatedSenses,
        List<(Guid VocabId, Guid SenseId)> resultSenseIds,
        List<(VocabularyEntity Vocab, VocabularySenseEntity Sense)> inMemoryFallbacks,
        CancellationToken ct)
    {
        string userId = context.UserId;
        var now = _timeProvider.GetUtcNow();

        var audit = new VocabularyGenerationAuditEntity(
            userId,
            context.ModelCode,
            VocabularyGenerationDefaults.PromptVersion,
            context.RequestedCount);

        var normalizedWords = generatedSenses
            .Select(s => VocabularyEntity.NormalizeWord(s.Word))
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Distinct()
            .ToList();

        var existingVocabularies = await db.Vocabularies
            .Include(v => v.Senses)
            .Where(v => v.LanguageCode == "en" && normalizedWords.Contains(v.NormalizedWord))
            .ToListAsync(ct);

        var vocabularies = existingVocabularies
            .GroupBy(v => v.NormalizedWord)
            .ToDictionary(g => g.Key, g => g.OrderBy(v => v.CreatedAtUtc).ThenBy(v => v.Id).First());

        var (accepted, rejected) = await PopulateVocabularyGraphAsync(
            context,
            generatedSenses,
            vocabularies,
            addVocabAsync: v => { db.Vocabularies.Add(v); return Task.CompletedTask; },
            addSenseAsync: s => { db.VocabularySenses.Add(s); return Task.CompletedTask; },
            resultSenseIds,
            inMemoryFallbacks);

        if (context.AssignToTodayPlan && resultSenseIds.Count > 0)
        {
            string today = LearningDate.ResolveLocalDateString(context.TimeZoneId, now);
            var todayPlan = await db.DailyVocabularyPlans
                .Include(p => p.Items)
                .FirstOrDefaultAsync(p => p.UserId == userId && p.LearningDate == today, ct);

            if (todayPlan is null)
            {
                todayPlan = new DailyVocabularyPlanEntity(
                    userId,
                    today,
                    context.TimeZoneId,
                    newVocabularyTarget: context.RequestedCount,
                    currentUtcTime: now);
                db.DailyVocabularyPlans.Add(todayPlan);
            }

            int nextOrder = todayPlan.Items.Count == 0 ? 1 : todayPlan.Items.Max(i => i.DisplayOrder) + 1;
            foreach (var (vId, sId) in resultSenseIds)
            {
                if (!todayPlan.Items.Any(i => i.SenseId == sId))
                {
                    var addedItem = todayPlan.AddItem(vId, sId, "New", nextOrder++, now);
                    db.DailyVocabularyItems.Add(addedItem);
                }
            }
        }

        audit.MarkCompleted(accepted, rejected);
        db.VocabularyGenerationAudits.Add(audit);
    }

    private static async Task<(int Accepted, int Rejected)> PopulateVocabularyGraphAsync(
        VocabularyGenerationContext context,
        List<GeneratedVocabularyRawSense> generatedSenses,
        Dictionary<string, VocabularyEntity> vocabularies,
        Func<VocabularyEntity, Task> addVocabAsync,
        Func<VocabularySenseEntity, Task> addSenseAsync,
        List<(Guid VocabId, Guid SenseId)> resultSenseIds,
        List<(VocabularyEntity Vocab, VocabularySenseEntity Sense)> inMemoryFallbacks)
    {
        int accepted = 0;
        int rejected = 0;
        int senseOrderCounter = 1;
        string cefr = context.CefrLevel;
        string topic = context.Topic;

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

                await addVocabAsync(vocab);
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
                {
                    resultSenseIds.Add((vocab.Id, duplicateSense.Id));
                    inMemoryFallbacks.Add((vocab, duplicateSense));
                }
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
                await addSenseAsync(sense);
            }

            resultSenseIds.Add((vocab.Id, sense.Id));
            inMemoryFallbacks.Add((vocab, sense));
            accepted++;
        }

        return (accepted, rejected);
    }

    public async Task RecordFailureAsync(VocabularyGenerationContext context, Exception error)
    {
        try
        {
            if (_failureAuditWriter is not null)
            {
                await _failureAuditWriter.RecordFailedRunAsync(
                    context.UserId,
                    context.ModelCode,
                    VocabularyGenerationDefaults.PromptVersion,
                    context.RequestedCount,
                    error.Message,
                    CancellationToken.None);
                return;
            }

            if (_legacyDbFactory is not null)
            {
                await using var db = await _legacyDbFactory.CreateDbContextAsync(CancellationToken.None);
                var audit = new VocabularyGenerationAuditEntity(
                    context.UserId,
                    context.ModelCode,
                    VocabularyGenerationDefaults.PromptVersion,
                    context.RequestedCount);
                audit.MarkFailed(error.Message);
                db.VocabularyGenerationAudits.Add(audit);
                await db.SaveChangesAsync(CancellationToken.None);
            }
        }
        catch (Exception auditError)
        {
            _logger.LogWarning(auditError, "Unable to save the failed vocabulary generation audit.");
        }
    }
}
