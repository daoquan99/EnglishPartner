using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Grammar.Models;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.Admin.PromoteGrammarExercise;

public sealed record PromoteGrammarExerciseCommand(
    Guid Id,
    PromoteGrammarExerciseRequest Request
) : ICommand<GrammarExerciseAdminDto>;
