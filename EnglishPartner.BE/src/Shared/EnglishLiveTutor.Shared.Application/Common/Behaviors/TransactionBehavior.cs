using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Persistence;
using MediatR;

namespace EnglishLiveTutor.Application.Common.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (request is not ITransactionalCommand)
        {
            return await next(ct);
        }

        return await unitOfWork.ExecuteInTransactionAsync(
            async token => await next(token),
            ct);
    }
}
