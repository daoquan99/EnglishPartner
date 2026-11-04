using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.AdminUpdateUser;

public sealed class AdminUpdateUserValidator : AbstractValidator<AdminUpdateUserCommand>
{
    public AdminUpdateUserValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId không được để trống.");
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");
    }
}
