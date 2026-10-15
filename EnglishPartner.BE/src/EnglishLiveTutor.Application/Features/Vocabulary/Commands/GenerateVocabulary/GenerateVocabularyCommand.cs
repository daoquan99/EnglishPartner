using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Learning;
using EnglishLiveTutor.Domain.Vocabulary;
using EnglishLiveTutor.Domain.Vocabulary.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Commands.GenerateVocabulary;

public sealed record GenerateVocabularyCommand(
    string UserId,
    GenerateVocabularyRequest Request
) : ICommand<List<VocabularyFlashcardDto>>;

public sealed class GenerateVocabularyCommandHandler : ICommandHandler<GenerateVocabularyCommand, List<VocabularyFlashcardDto>>
{
    private readonly IAppDbContextFactory _dbFactory;
    private readonly IVocabularyAiGenerator _aiGenerator;

    public const string PromptVersion = "PROMPT_VOCAB_GEN_V1";

    public GenerateVocabularyCommandHandler(
        IAppDbContextFactory dbFactory,
        IVocabularyAiGenerator aiGenerator)
    {
        _dbFactory = dbFactory;
        _aiGenerator = aiGenerator;
    }

    public async Task<List<VocabularyFlashcardDto>> HandleAsync(GenerateVocabularyCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var userId = command.UserId;

        if (string.IsNullOrWhiteSpace(req.ApiKey))
            throw new InvalidOperationException("Cần cung cấp API Key của Google Gemini để sinh từ vựng AI.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        string requestedModelCode = string.IsNullOrWhiteSpace(req.ModelCode) ? "gemini-3.8-flash" : req.ModelCode.Trim();

        var modelMeta = await db.AiModelCatalog
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.ModelCode == requestedModelCode, ct);

        if (modelMeta is null)
            throw new InvalidOperationException($"Model '{requestedModelCode}' không tồn tại trong AI Model Catalog.");

        if (!modelMeta.IsEnabled || !modelMeta.IsAvailable || !modelMeta.SupportsTextGeneration || modelMeta.SupportsLiveAudio)
            throw new InvalidOperationException($"Model '{modelMeta.DisplayName}' không hỗ trợ tạo từ vựng cấu trúc JSON.");

        string effectiveModelCode = modelMeta.GetEffectiveModelCode();

        int requestedCount = Math.Clamp(req.RequestedCount, 1, 10);
        string cefr = string.IsNullOrWhiteSpace(req.CefrLevel) ? "B1" : req.CefrLevel.Trim().ToUpperInvariant();
        string topic = string.IsNullOrWhiteSpace(req.Topic) ? "General English & Career Communication" : req.Topic.Trim();

        var existingWords = await db.Vocabularies
            .AsNoTracking()
            .Select(v => v.NormalizedWord)
            .ToListAsync(ct);

        var excludedList = existingWords
            .Concat(req.ExcludedWords ?? new List<string>())
            .Distinct()
            .Take(80)
            .ToList();

        var audit = new VocabularyGenerationAuditEntity(
            userId: userId,
            modelCode: requestedModelCode,
            promptVersion: PromptVersion,
            requestedCount: requestedCount);

        var createdSenseIds = new List<(Guid VocabId, Guid SenseId)>();

        try
        {
            var generatedSenses = await _aiGenerator.GenerateVocabularyAsync(
                req.ApiKey.Trim(),
                effectiveModelCode,
                cefr,
                topic,
                requestedCount,
                excludedList,
                ct);

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

                var existingVocab = await db.Vocabularies
                    .Include(v => v.Senses)
                    .FirstOrDefaultAsync(v => v.NormalizedWord == normWord, ct);

                VocabularyEntity vocab;
                if (existingVocab is null)
                {
                    vocab = VocabularyEntity.Create(
                        word: wordRaw,
                        languageCode: "en",
                        sourceType: nameof(VocabularySourceType.AI),
                        sourceModelId: requestedModelCode,
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
                }
                else
                {
                    vocab = existingVocab;
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

                createdSenseIds.Add((vocab.Id, sense.Id));
                accepted++;
            }

            audit.MarkCompleted(accepted, rejected);
            db.VocabularyGenerationAudits.Add(audit);

            await db.SaveChangesAsync(ct);

            // Optional: assign to today's plan immediately
            if (req.AssignToTodayPlan && createdSenseIds.Count > 0)
            {
                string today = DailyVocabularyPlanEntity.ResolveLocalLearningDate("Asia/Ho_Chi_Minh");
                var todayPlan = await db.DailyVocabularyPlans
                    .Include(p => p.Items)
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.LearningDate == today, ct);

                if (todayPlan is not null)
                {
                    int nextOrder = todayPlan.Items.Count == 0 ? 1 : todayPlan.Items.Max(i => i.DisplayOrder) + 1;
                    foreach (var (vId, sId) in createdSenseIds)
                    {
                        if (!todayPlan.Items.Any(i => i.SenseId == sId))
                        {
                            var addedItem = todayPlan.AddItem(vId, sId, "New", nextOrder++, DateTimeOffset.UtcNow);
                            db.DailyVocabularyItems.Add(addedItem);
                        }
                    }
                    await db.SaveChangesAsync(ct);
                }
            }

            var resultDtos = new List<VocabularyFlashcardDto>();
            foreach (var (vId, sId) in createdSenseIds)
            {
                var card = await VocabularyProjectionHelper.GetFlashcardByVocabularyIdAsync(db, vId, sId, userId, ct);
                if (card is not null) resultDtos.Add(card);
            }

            return resultDtos;
        }
        catch (Exception ex)
        {
            audit.MarkFailed(ex.Message);
            db.VocabularyGenerationAudits.Add(audit);
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }
}
