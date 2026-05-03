using FluentValidation;

namespace EnglishPartner.Modules.Identity.Application.Admin.Users.Commands.ManageUserRole;

public sealed class ManageUserRoleCommandValidator : AbstractValidator<ManageUserRoleCommand>
{
    private static readonly string[] ValidOperations = ["Assign", "Revoke"];

    public ManageUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.Operation)
            .NotEmpty()
            .Must(op => ValidOperations.Contains(op, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Operation must be 'Assign' or 'Revoke'.");
    }
}
