using System.Reflection;
using EnglishPartner.Modules.Curriculum.Application.Abstractions;
using EnglishPartner.SharedKernel.CQRS;
using MediatR;

namespace EnglishPartner.Modules.Curriculum.Application.Behaviors;

internal sealed class CurriculumUnitOfWorkBehavior<TRequest, TResponse>(
    ICurriculumUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly Assembly ModuleAssembly = typeof(CurriculumUnitOfWorkBehavior<,>).Assembly;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (typeof(TRequest).Assembly != ModuleAssembly)
            return await next();

        if (!IsCommand())
            return await next();

        var response = await next();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return response;
    }

    private static bool IsCommand()
    {
        return typeof(ICommand).IsAssignableFrom(typeof(TRequest))
            || typeof(TRequest).GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));
    }
}
