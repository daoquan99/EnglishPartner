---
trigger: manual
---

# Common: Validation & Results

> **Scope:** FluentValidation, ValidationBehavior, Result<T>, Error
> @mention this file when working on: validation, returning errors, mapping Result to ProblemDetails

---

## 1. Result Pattern (`Result<T>`)

Instead of throwing exceptions, all Handlers return a `Result` or `Result<T>`.

### Creating Results
```csharp
// Success
return Result.Success();
return Result.Success(entity.Id); // implicitly converts to Result<Guid>

// Failure
return Result.Failure(FooErrors.NotFound);
return Result.Failure<FooResponse>(FooErrors.AlreadyExists);
```

### Mapping to HTTP Responses (Minimal APIs)
Use `.ToProblemDetails()` in the API endpoint:
```csharp
var result = await sender.Send(command);
return result.IsSuccess 
    ? Results.Ok(result.Value) 
    : result.ToProblemDetails();
```

---

## 2. Defining Errors (`Error`)

Define errors as static constants in a class like `{Entity}Errors.cs` inside the Domain layer.

```csharp
public static class UserErrors
{
    // 404 Not Found
    public static readonly Error NotFound = Error.NotFound("User.NotFound", "The user was not found.");
    
    // 409 Conflict
    public static readonly Error EmailAlreadyExists = Error.Conflict("User.EmailAlreadyExists", "Email is already taken.");
    
    // 400 Bad Request
    public static readonly Error InvalidCredentials = Error.Failure("User.InvalidCredentials", "Invalid email or password.");
}
```

---

## 3. FluentValidation & ValidationBehavior

**CRITICAL RULE:** Do NOT perform manual validation (like `string.IsNullOrEmpty`) inside Command/Query Handlers. 

Instead, create an `AbstractValidator<T>` for your Command/Query. The MediatR `ValidationBehavior` will automatically intercept the request, run the validator, and return a `Result.Failure` containing a `ValidationError` (400 Bad Request) if it fails.

```csharp
// Command
public sealed record CreateFooCommand(string Name, int Age) : ICommand<Guid>;

// Validator (put in the same file or a Validators folder)
internal sealed class CreateFooCommandValidator : AbstractValidator<CreateFooCommand>
{
    public CreateFooCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithError(Error.Validation("Foo.NameRequired", "Name is required."))
            .MaximumLength(100);

        RuleFor(x => x.Age)
            .GreaterThan(18).WithMessage("Must be older than 18.");
    }
}
```

**How it works:**
If the validation fails, the Handler is NEVER executed. The API automatically returns a `ProblemDetails` with status 400 and a list of specific property errors.
