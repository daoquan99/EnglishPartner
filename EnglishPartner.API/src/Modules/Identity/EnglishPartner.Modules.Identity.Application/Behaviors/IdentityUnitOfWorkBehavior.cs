using System.Reflection;
using EnglishPartner.Modules.Identity.Application.Abstractions;
using EnglishPartner.SharedKernel.CQRS;
using MediatR;

namespace EnglishPartner.Modules.Identity.Application.Behaviors;

/// <summary>
/// Module-scoped UnitOfWork behavior for Identity.
/// Only fires for commands from this module's assembly.
/// Depends on IIdentityUnitOfWork (implemented by IdentityDbContext in Infrastructure).
/// </summary>
internal sealed class IdentityUnitOfWorkBehavior<TRequest, TResponse>(
    IIdentityUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly Assembly ModuleAssembly = typeof(IdentityUnitOfWorkBehavior<,>).Assembly;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Skip if this request doesn't belong to the Identity module
        if (typeof(TRequest).Assembly != ModuleAssembly)
            return await next();

        // Skip if this is a query (read-only)
        if (request is not IBaseCommand)
            return await next();

        var response = await next();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return response;
    }
}
