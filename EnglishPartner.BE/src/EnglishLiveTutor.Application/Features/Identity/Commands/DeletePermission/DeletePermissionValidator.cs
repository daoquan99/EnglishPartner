using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.DeletePermission;

public sealed class DeletePermissionValidator : AbstractValidator<DeletePermissionCommand>
{
    public DeletePermissionValidator()
    {
        RuleFor(x => x.PermissionId).NotEmpty().WithMessage("PermissionId khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
    }
}
