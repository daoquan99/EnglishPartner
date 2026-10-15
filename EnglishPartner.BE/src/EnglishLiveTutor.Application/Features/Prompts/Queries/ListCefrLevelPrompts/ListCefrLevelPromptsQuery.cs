using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.Prompts;
using Microsoft.EntityFrameworkCore;

namespace EnglishLiveTutor.Application.Features.Prompts.Queries.ListCefrLevelPrompts;

public sealed record ListCefrLevelPromptsQuery(
    string? Search = null,
    int? Page = null,
    int? PageSize = null
) : IQuery<PagedResultDto<CefrLevelPromptEntity>>;
