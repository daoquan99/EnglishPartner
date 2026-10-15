using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.UpdatePermission;

public sealed class UpdatePermissionValidator : AbstractValidator<UpdatePermissionCommand>
{
    public UpdatePermissionValidator()
    {
        RuleFor(x => x.PermissionId).NotEmpty().WithMessage("PermissionId khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
        RuleFor(x => x.Request).NotNull().WithMessage("Request khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
    }
}
