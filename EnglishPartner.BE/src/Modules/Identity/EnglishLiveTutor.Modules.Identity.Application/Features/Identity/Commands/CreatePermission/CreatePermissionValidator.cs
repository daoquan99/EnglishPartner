using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.CreatePermission;

public sealed class CreatePermissionValidator : AbstractValidator<CreatePermissionCommand>
{
    public CreatePermissionValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");
    }
}
