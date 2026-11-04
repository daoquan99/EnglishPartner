using FluentValidation.Results;

namespace EnglishLiveTutor.Application.Common.Validation;

public class ValidationException : InvalidOperationException
{
    public IReadOnlyList<ValidationFailure> Errors { get; }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : base(BuildErrorMessage(failures))
    {
        Errors = failures.ToList().AsReadOnly();
    }

    private static string BuildErrorMessage(IEnumerable<ValidationFailure> failures)
    {
        return string.Join("; ", failures.Select(f => f.ErrorMessage));
    }
}
