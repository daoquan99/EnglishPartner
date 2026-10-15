using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Vocabulary;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Queries.BuildVocabularyGenerationPrompt;

public sealed class BuildVocabularyGenerationPromptQueryHandler(IAppDbContextFactory dbFactory, IVocabularyGenerationContent content)
    : IQueryHandler<BuildVocabularyGenerationPromptQuery, VocabularyGenerationPromptDto>
{
    public async Task<VocabularyGenerationPromptDto> HandleAsync(BuildVocabularyGenerationPromptQuery query, CancellationToken ct = default)
    {
        var req = query.Request;
        int count = Math.Clamp(req.RequestedCount, 1, VocabularyGenerationDefaults.MaxCount);
        string cefr = VocabularyGenerationDefaults.NormalizeCefr(req.CefrLevel);
        string topic = VocabularyGenerationDefaults.NormalizeTopic(req.Topic);
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var savedWords = await db.Vocabularies.AsNoTracking()
            .Select(v => v.NormalizedWord).Distinct().OrderBy(w => w).Take(80).ToListAsync(ct);
        var excluded = (req.ExcludedWords ?? [])
            .Concat(savedWords)
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Select(VocabularyEntity.NormalizeWord)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(80).ToList();
        return new VocabularyGenerationPromptDto(content.BuildPrompt(cefr, topic, count, excluded),
            VocabularyGenerationDefaults.PromptVersion, count, cefr, topic, excluded);
    }
}
