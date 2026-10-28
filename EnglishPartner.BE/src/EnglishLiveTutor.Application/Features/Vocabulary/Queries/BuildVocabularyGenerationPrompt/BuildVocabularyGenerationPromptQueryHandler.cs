using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Vocabulary.Services;

namespace EnglishLiveTutor.Application.Features.Vocabulary.Queries.BuildVocabularyGenerationPrompt;

public sealed class BuildVocabularyGenerationPromptQueryHandler : IQueryHandler<BuildVocabularyGenerationPromptQuery, VocabularyGenerationPromptDto>
{
    private readonly IVocabularyPromptBuilder _promptBuilder;

    public BuildVocabularyGenerationPromptQueryHandler(IVocabularyPromptBuilder promptBuilder)
    {
        _promptBuilder = promptBuilder;
    }

    public async Task<VocabularyGenerationPromptDto> HandleAsync(BuildVocabularyGenerationPromptQuery query, CancellationToken ct = default)
    {
        return await _promptBuilder.BuildPromptAsync(query.Request, ct);
    }
}
