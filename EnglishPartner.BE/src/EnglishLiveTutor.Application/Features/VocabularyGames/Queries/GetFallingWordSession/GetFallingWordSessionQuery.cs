using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Queries.GetFallingWordSession;

public sealed record GetFallingWordSessionQuery(
    Guid SessionId) : IQuery<FallingWordSessionDto?>;
