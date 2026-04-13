namespace EnglishPartner.Modules.AI.Domain.AiRequestLogs.Repositories;

public interface IAiRequestLogRepository
{
    Task<(IReadOnlyList<AiRequestLog> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? requestType = null,
        bool? isSuccess = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);
}
