using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using MediatR;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.Admin.RejectGrammarExercise;

public sealed record RejectGrammarExerciseCommand(
    Guid Id,
    RejectGrammarExerciseRequest Request
) : ICommand<Unit>;
