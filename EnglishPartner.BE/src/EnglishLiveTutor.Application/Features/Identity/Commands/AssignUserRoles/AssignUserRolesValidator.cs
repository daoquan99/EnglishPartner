using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.AssignUserRoles;

public sealed class AssignUserRolesValidator : AbstractValidator<AssignUserRolesCommand>
{
    public AssignUserRolesValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId không được để trống.");
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");
    }
}
