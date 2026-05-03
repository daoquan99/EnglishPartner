namespace EnglishPartner.SharedKernel.Results;

/// <summary>
/// Represents a domain error with a code and message.
/// Modules define their own static error classes inheriting this pattern.
/// </summary>
public record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "A null value was provided.");
}

/// <summary>
/// Common errors shared across all modules.
/// Module-specific errors should be defined in each module's Domain layer.
/// Example: IdentityErrors.cs, CurriculumErrors.cs
/// </summary>
public static class CommonErrors
{
    public static Error NotFound(string entityName, Guid id)
        => new($"{entityName}.NotFound", $"{entityName} with Id '{id}' was not found.");

    public static Error NotFound(string entityName, string identifier)
        => new($"{entityName}.NotFound", $"{entityName} '{identifier}' was not found.");

    public static readonly Error Unauthorized =
        new("Error.Unauthorized", "You are not authorized to perform this action.");

    public static readonly Error Forbidden =
        new("Error.Forbidden", "You do not have permission to access this resource.");

    public static Error Validation(string code, string message)
        => new(code, message);

    public static Error Conflict(string code, string message)
        => new(code, message);

    public static Error Failure(string code, string message)
        => new(code, message);

    public static readonly Error InternalError =
        new("Error.Internal", "An unexpected error occurred.");
}
