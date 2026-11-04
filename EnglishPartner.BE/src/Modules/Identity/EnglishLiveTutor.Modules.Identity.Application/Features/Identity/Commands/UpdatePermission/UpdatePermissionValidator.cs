using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.UpdatePermission;

public sealed class UpdatePermissionValidator : AbstractValidator<UpdatePermissionCommand>
{
    public UpdatePermissionValidator()
    {
        RuleFor(x => x.PermissionId).NotEmpty().WithMessage("PermissionId không được để trống.");
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");
    }
}
