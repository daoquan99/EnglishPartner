using EnglishPartner.SharedKernel.Results;
using FluentValidation;
using MediatR;

namespace EnglishPartner.SharedKernel.Behaviors;

/// <summary>
/// Runs FluentValidation validators before handler execution.
/// Returns Result.Failure(ValidationError) on validation failure.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var errors = failures
            .Select(f => new Error(f.PropertyName, f.ErrorMessage))
            .ToArray();

        if (typeof(TResponse) == typeof(Result))
            return (TResponse)(object)Result.Failure(new ValidationError(errors));

        var resultType = typeof(TResponse);
        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failureMethod = typeof(Result)
                .GetMethod(nameof(Result.Failure), 1, [typeof(Error)])!
                .MakeGenericMethod(resultType.GetGenericArguments()[0]);

            return (TResponse)failureMethod.Invoke(null, [new ValidationError(errors)])!;
        }

        throw new ValidationException(failures);
    }
}
