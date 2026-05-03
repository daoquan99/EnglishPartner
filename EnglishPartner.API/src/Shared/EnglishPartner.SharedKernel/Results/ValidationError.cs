namespace EnglishPartner.SharedKernel.Results;

public sealed record ValidationError(Error[] Errors)
    : Error("Validation.Failed", "One or more validation errors occurred.")
{
    public static ValidationError FromErrors(IEnumerable<Error> errors)
        => new(errors.ToArray());
}
