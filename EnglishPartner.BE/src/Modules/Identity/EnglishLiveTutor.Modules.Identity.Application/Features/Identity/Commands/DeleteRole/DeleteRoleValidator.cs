using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.DeleteRole;

public sealed class DeleteRoleValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty().WithMessage("RoleId không được để trống.");
    }
}
