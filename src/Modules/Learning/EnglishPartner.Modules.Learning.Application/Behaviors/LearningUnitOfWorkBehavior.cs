using EnglishPartner.Modules.Learning.Application.Abstractions;
using EnglishPartner.SharedKernel.CQRS;
using MediatR;

namespace EnglishPartner.Modules.Learning.Application.Behaviors;

internal sealed class LearningUnitOfWorkBehavior<TRequest, TResponse>(
    ILearningUnitOfWork unitOfWork) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (IsNotCommand())
        {
            return await next();
        }

        var response = await next();

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }

    private static bool IsNotCommand()
    {
        return !typeof(TRequest).Name.EndsWith("Command");
    }
}
