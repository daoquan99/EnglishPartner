using EnglishPartner.Modules.Notifications.Application.Abstractions;
using EnglishPartner.SharedKernel.CQRS;
using MediatR;

namespace EnglishPartner.Modules.Notifications.Application.Behaviors;

/// <summary>
/// Module-scoped Unit of Work behavior for Notifications.
/// Calls SaveChangesAsync after every Notification command succeeds.
/// </summary>
internal sealed class NotificationUnitOfWorkBehavior<TRequest, TResponse>(
    INotificationUnitOfWork unitOfWork) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not IBaseCommand)
            return await next();

        var response = await next();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return response;
    }
}
