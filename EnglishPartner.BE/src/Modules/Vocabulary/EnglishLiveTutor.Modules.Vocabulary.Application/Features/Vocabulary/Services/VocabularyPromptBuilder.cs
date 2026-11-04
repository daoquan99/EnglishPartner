using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Modules;
using EnglishLiveTutor.Application.Modules.Vocabulary;
using EnglishLiveTutor.Domain.Vocabulary;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Services;

public interface IVocabularyPromptBuilder
{
    Task<VocabularyGenerationPromptDto> BuildPromptAsync(BuildVocabularyGenerationPromptRequest req, CancellationToken ct = default);
}

public sealed class VocabularyPromptBuilder(IModuleDbContextFactory<IVocabularyDbContext> dbFactory, IVocabularyGenerationContent content)
    : IVocabularyPromptBuilder
{
    public async Task<VocabularyGenerationPromptDto> BuildPromptAsync(BuildVocabularyGenerationPromptRequest req, CancellationToken ct = default)
    {
        string cefr = VocabularyGenerationDefaults.NormalizeCefr(req.CefrLevel);
        string topic = VocabularyGenerationDefaults.NormalizeTopic(req.Topic);
        int examplesPerSense = Math.Clamp(req.ExamplesPerSense, 1, 5);

        var cleanTargetTerms = TargetedVocabularyHelper.CleanTargetTerms(req.TargetTerms);
        string? cleanSourceSentence = string.IsNullOrWhiteSpace(req.SourceSentence) ? null : req.SourceSentence.Trim();

        if (cleanTargetTerms.Count > 0)
        {
            int count = cleanTargetTerms.Count;
            // TargetTerms branch: Do NOT apply excluded words list to the requested target terms
            string promptText = content.BuildPrompt(new VocabularyPromptOptions(
                CefrLevel: cefr,
                Topic: topic,
                Count: count,
                ExcludedWords: [],
                ExamplesPerSense: examplesPerSense,
                TargetTerms: cleanTargetTerms,
                SourceSentence: null));

            return new VocabularyGenerationPromptDto(
                Prompt: promptText,
                PromptVersion: VocabularyGenerationDefaults.PromptVersion,
                RequestedCount: count,
                CefrLevel: cefr,
                Topic: topic,
                ExcludedWords: []);
        }

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var savedWords = await db.Vocabularies.AsNoTracking()
            .Select(v => v.NormalizedWord).Distinct().OrderBy(w => w).Take(80).ToListAsync(ct);
        var excluded = (req.ExcludedWords ?? [])
            .Concat(savedWords)
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Select(VocabularyEntity.NormalizeWord)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(80).ToList();

        if (cleanSourceSentence != null)
        {
            int count = Math.Clamp(req.RequestedCount, 1, 5);
            string promptText = content.BuildPrompt(new VocabularyPromptOptions(
                CefrLevel: cefr,
                Topic: topic,
                Count: count,
                ExcludedWords: excluded,
                ExamplesPerSense: examplesPerSense,
                TargetTerms: null,
                SourceSentence: cleanSourceSentence));

            return new VocabularyGenerationPromptDto(
                Prompt: promptText,
                PromptVersion: VocabularyGenerationDefaults.PromptVersion,
                RequestedCount: count,
                CefrLevel: cefr,
                Topic: topic,
                ExcludedWords: excluded);
        }

        int defaultCount = Math.Clamp(req.RequestedCount, 1, VocabularyGenerationDefaults.MaxCount);
        string defaultPrompt = content.BuildPrompt(new VocabularyPromptOptions(
            CefrLevel: cefr,
            Topic: topic,
            Count: defaultCount,
            ExcludedWords: excluded,
            ExamplesPerSense: examplesPerSense,
            TargetTerms: null,
            SourceSentence: null));

        return new VocabularyGenerationPromptDto(
            Prompt: defaultPrompt,
            PromptVersion: VocabularyGenerationDefaults.PromptVersion,
            RequestedCount: defaultCount,
            CefrLevel: cefr,
            Topic: topic,
            ExcludedWords: excluded);
    }
}
