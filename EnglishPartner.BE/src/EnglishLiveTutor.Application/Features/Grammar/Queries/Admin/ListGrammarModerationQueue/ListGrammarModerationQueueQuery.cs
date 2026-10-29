using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Domain.Grammar.Enums;

namespace EnglishLiveTutor.Application.Features.Grammar.Queries.Admin.ListGrammarModerationQueue;

public sealed record ListGrammarModerationQueueQuery(
    GrammarModerationStatus Status = GrammarModerationStatus.Pending,
    string? Level = null,
    string? StepCode = null,
    int? Page = null,
    int? PageSize = null,
    bool? Paged = null
) : IQuery<PagedResultDto<GrammarModerationItemDto>>;
