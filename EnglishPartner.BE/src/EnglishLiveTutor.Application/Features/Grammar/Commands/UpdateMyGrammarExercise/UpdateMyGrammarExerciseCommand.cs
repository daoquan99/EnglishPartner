using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Grammar.Models;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.UpdateMyGrammarExercise;

public sealed record UpdateMyGrammarExerciseCommand(
    Guid Id,
    UpdateMyGrammarExerciseRequest Request
) : ICommand<GrammarMyExerciseDto>;
