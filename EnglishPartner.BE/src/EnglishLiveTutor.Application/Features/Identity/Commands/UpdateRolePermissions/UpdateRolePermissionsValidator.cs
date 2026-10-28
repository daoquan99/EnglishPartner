using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.UpdateRolePermissions;

public sealed class UpdateRolePermissionsValidator : AbstractValidator<UpdateRolePermissionsCommand>
{
    public UpdateRolePermissionsValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty().WithMessage("RoleId không được để trống.");
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");
    }
}
