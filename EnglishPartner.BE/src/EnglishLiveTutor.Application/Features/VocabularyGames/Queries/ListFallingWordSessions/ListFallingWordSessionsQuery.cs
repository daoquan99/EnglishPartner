using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;
using EnglishLiveTutor.Domain.VocabularyGames.Enums;

namespace EnglishLiveTutor.Application.Features.VocabularyGames.Queries.ListFallingWordSessions;

public sealed record ListFallingWordSessionsQuery(
    int Page = 1,
    int PageSize = 20,
    FallingWordGameSource? Source = null,
    FallingWordGameStatus? Status = null) : IQuery<PagedResultDto<FallingWordSessionSummaryDto>>;
