using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Grammar.Models;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.ChangeMyGrammarExerciseVisibility;

public sealed record ChangeMyGrammarExerciseVisibilityCommand(
    Guid Id,
    ChangeMyGrammarExerciseVisibilityRequest Request
) : ICommand<GrammarMyExerciseDto>;
