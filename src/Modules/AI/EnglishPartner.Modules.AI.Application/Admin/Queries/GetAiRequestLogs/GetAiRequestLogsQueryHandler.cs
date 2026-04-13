using EnglishPartner.Modules.AI.Domain.AiRequestLogs.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Pagination;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.AI.Application.Admin.Queries.GetAiRequestLogs;

internal sealed class GetAiRequestLogsQueryHandler(
    IAiRequestLogRepository logRepository) : IQueryHandler<GetAiRequestLogsQuery, PagedResult<AiRequestLogDto>>
{
    public async Task<Result<PagedResult<AiRequestLogDto>>> Handle(GetAiRequestLogsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await logRepository.GetPagedAsync(
            request.Page,
            request.PageSize,
            request.RequestType,
            request.IsSuccess,
            request.From,
            request.To,
            cancellationToken);

        var dtos = items.Select(x => new AiRequestLogDto(
            x.Id,
            x.RequestType,
            x.InputText,
            x.OutputText,
            x.ModelName,
            x.LatencyMs,
            x.InputTokens,
            x.OutputTokens,
            x.IsSuccess,
            x.ErrorMessage,
            x.CreatedAt)).ToList();

        return new PagedResult<AiRequestLogDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}
