using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;
using EnglishLiveTutor.Domain.VocabularyGames.Enums;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Queries.GetFallingWordPool;

public sealed record GetFallingWordPoolQuery(
    FallingWordGameSource Source = FallingWordGameSource.VocabularyCatalog,
    string? CefrLevel = null,
    FallingWordPlayFilter PlayFilter = FallingWordPlayFilter.All,
    string? TimeZoneId = null) : IQuery<FallingWordPoolCountDto>;
