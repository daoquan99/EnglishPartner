using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Grammar.Models;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.ImportGrammarExercisesJson;

public sealed record ImportGrammarExercisesJsonCommand(
    string StepCode,
    ImportGrammarExercisesJsonRequest Request
) : ICommand<GenerateGrammarExercisesResponse>;
