using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Queries.ListFallingWordSessionRounds;

public sealed record ListFallingWordSessionRoundsQuery(
    Guid SessionId,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResultDto<TerminalFallingWordRoundDto>>;
