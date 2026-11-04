namespace EnglishLiveTutor.Application.Common.Persistence;

public interface IUnitOfWork
{
    Task<int> CommitChangesAsync(CancellationToken ct = default);

    Task<TResponse> ExecuteInTransactionAsync<TResponse>(
        Func<CancellationToken, Task<TResponse>> operation,
        CancellationToken ct = default);
}
