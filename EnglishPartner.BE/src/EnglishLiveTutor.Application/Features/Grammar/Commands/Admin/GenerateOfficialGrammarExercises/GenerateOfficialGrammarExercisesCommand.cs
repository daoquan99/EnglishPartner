using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Grammar.Models;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.Admin.GenerateOfficialGrammarExercises;

public sealed record GenerateOfficialGrammarExercisesResponse(
    IReadOnlyList<GrammarExerciseAdminDto> Created,
    IReadOnlyList<GrammarExerciseRejectionDto> Rejected
);

public sealed record GenerateOfficialGrammarExercisesCommand(
    string StepCode,
    GenerateOfficialGrammarExercisesRequest Request,
    string? ImportJsonText = null
) : ICommand<GenerateOfficialGrammarExercisesResponse>;
