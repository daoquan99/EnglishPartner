using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Pronunciation;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Pronunciation.Queries.ListScoringRubrics;

public sealed record ListScoringRubricsQuery(string? Search = null, int? Page = null, int? PageSize = null) : IQuery<PagedResultDto<ScoringRubricEntity>>;
