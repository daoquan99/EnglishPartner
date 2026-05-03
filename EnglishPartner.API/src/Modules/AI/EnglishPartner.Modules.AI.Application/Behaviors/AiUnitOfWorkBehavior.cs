using System.Reflection;
using EnglishPartner.Modules.AI.Application.Abstractions;
using EnglishPartner.SharedKernel.CQRS;
using MediatR;

namespace EnglishPartner.Modules.AI.Application.Behaviors;

/// <summary>
/// Auto-calls SaveChangesAsync on AI module's DbContext after command execution.
/// </summary>
internal sealed class AiUnitOfWorkBehavior<TRequest, TResponse>(
    IAiUnitOfWork unitOfWork) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly Assembly ModuleAssembly = typeof(AiUnitOfWorkBehavior<,>).Assembly;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestAssembly = typeof(TRequest).Assembly;

        if (!ModuleAssembly.Equals(requestAssembly))
            return await next();

        if (request is not IBaseCommand)
            return await next();

        var response = await next();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return response;
    }
}
