using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Application.Features.Grammar.Services;

namespace EnglishLiveTutor.Application.Features.Grammar.Queries.BuildGrammarExercisePrompt;

public sealed class BuildGrammarExercisePromptQueryHandler
    : IQueryHandler<BuildGrammarExercisePromptQuery, BuildGrammarExercisePromptResponse>
{
    private readonly IGrammarExercisePromptContextLoader _contextLoader;
    private readonly IGrammarExercisePromptBuilder _promptBuilder;

    public BuildGrammarExercisePromptQueryHandler(
        IGrammarExercisePromptContextLoader contextLoader,
        IGrammarExercisePromptBuilder promptBuilder)
    {
        _contextLoader = contextLoader;
        _promptBuilder = promptBuilder;
    }

    public async Task<BuildGrammarExercisePromptResponse> HandleAsync(
        BuildGrammarExercisePromptQuery query,
        CancellationToken ct = default)
    {
        var promptInput = await _contextLoader.LoadContextAsync(
            query.StepCode,
            query.Request.Items,
            query.Request.LearnerNote,
            ct);

        string promptText = _promptBuilder.Build(promptInput);

        return new BuildGrammarExercisePromptResponse(promptText);
    }
}
