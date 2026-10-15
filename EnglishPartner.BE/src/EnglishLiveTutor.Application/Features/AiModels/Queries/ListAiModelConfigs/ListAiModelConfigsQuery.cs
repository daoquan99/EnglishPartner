using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Domain.AiCatalog;

namespace EnglishLiveTutor.Application.Features.AiModels.Queries.ListAiModelConfigs;

public sealed record ListAiModelConfigsQuery(string? Search = null, int? Page = null, int? PageSize = null) : IQuery<PagedResultDto<AiModelConfigEntity>>;
