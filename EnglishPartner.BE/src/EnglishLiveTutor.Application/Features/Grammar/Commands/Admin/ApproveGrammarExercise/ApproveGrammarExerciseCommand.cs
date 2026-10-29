using EnglishLiveTutor.Application.Common.Interfaces;
using MediatR;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.Admin.ApproveGrammarExercise;

public sealed record ApproveGrammarExerciseCommand(Guid Id) : ICommand<Unit>;
