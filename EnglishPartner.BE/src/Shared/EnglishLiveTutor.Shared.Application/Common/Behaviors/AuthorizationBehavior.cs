using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using MediatR;

namespace EnglishLiveTutor.Application.Common.Behaviors;

public sealed class AuthorizationBehavior<TRequest, TResponse>(
    IPermissionChecker permissionChecker)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (request is IRequirePermission requiredPermission)
        {
            var hasPermission = await permissionChecker.HasPermissionAsync(requiredPermission.Permission, ct);
            if (!hasPermission)
            {
                throw new ForbiddenException($"Bạn không có quyền '{requiredPermission.Permission}' để thực hiện thao tác này.");
            }
        }

        return await next(ct);
    }
}
