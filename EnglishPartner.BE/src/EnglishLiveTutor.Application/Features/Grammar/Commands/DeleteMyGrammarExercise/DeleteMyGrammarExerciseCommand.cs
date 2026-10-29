using EnglishLiveTutor.Application.Common.Interfaces;
using MediatR;

namespace EnglishLiveTutor.Application.Features.Grammar.Commands.DeleteMyGrammarExercise;

public sealed record DeleteMyGrammarExerciseCommand(Guid Id) : ICommand<Unit>;
