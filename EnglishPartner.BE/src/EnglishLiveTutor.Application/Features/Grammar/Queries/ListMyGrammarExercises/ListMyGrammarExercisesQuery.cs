using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Domain.Grammar.Enums;

namespace EnglishLiveTutor.Application.Features.Grammar.Queries.ListMyGrammarExercises;

public sealed record ListMyGrammarExercisesQuery(
    string? StepCode = null,
    GrammarModerationStatus? Moderation = null,
    int? Page = null,
    int? PageSize = null,
    bool? Paged = null
) : IQuery<PagedResultDto<GrammarMyExerciseDto>>;
