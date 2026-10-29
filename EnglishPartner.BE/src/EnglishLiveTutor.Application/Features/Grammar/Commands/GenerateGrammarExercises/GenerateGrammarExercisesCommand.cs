using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Grammar.Models;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.GenerateGrammarExercises;

public sealed record GenerateGrammarExercisesCommand(
    string StepCode,
    GenerateGrammarExercisesRequest Request
) : ICommand<GenerateGrammarExercisesResponse>;
