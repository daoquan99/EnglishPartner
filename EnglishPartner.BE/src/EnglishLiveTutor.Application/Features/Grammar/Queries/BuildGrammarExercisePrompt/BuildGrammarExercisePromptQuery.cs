using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Grammar.Models;

namespace EnglishLiveTutor.Application.Features.Grammar.Queries.BuildGrammarExercisePrompt;

public sealed record BuildGrammarExercisePromptQuery(
    string StepCode,
    BuildGrammarExercisePromptRequest Request,
    bool ForAdmin = false
) : IQuery<BuildGrammarExercisePromptResponse>;
