using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Commands.DeleteScoringRubric;

public sealed record DeleteScoringRubricCommand(string Code) : ICommand<bool>;
