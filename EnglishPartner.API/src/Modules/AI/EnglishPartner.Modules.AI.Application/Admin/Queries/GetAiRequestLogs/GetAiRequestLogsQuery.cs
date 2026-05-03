using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Pagination;

namespace EnglishPartner.Modules.AI.Application.Admin.Queries.GetAiRequestLogs;

public sealed record GetAiRequestLogsQuery(
    int Page,
    int PageSize,
    string? RequestType = null,
    bool? IsSuccess = null,
    DateTime? From = null,
    DateTime? To = null) : IQuery<PagedResult<AiRequestLogDto>>;

public sealed record AiRequestLogDto(
    Guid Id,
    string RequestType,
    string InputText,
    string OutputText,
    string ModelName,
    long LatencyMs,
    int? InputTokens,
    int? OutputTokens,
    bool IsSuccess,
    string? ErrorMessage,
    DateTime CreatedAt);
