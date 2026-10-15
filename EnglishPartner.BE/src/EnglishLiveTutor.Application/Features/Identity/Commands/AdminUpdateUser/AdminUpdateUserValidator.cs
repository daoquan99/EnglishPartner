using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.AdminUpdateUser;

public sealed class AdminUpdateUserValidator : AbstractValidator<AdminUpdateUserCommand>
{
    public AdminUpdateUserValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
        RuleFor(x => x.Request).NotNull().WithMessage("Request khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.");
    }
}
