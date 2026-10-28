using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.AdminCreateUser;

public sealed class AdminCreateUserValidator : AbstractValidator<AdminCreateUserCommand>
{
    public AdminCreateUserValidator()
    {
        RuleFor(x => x.Request)
            .NotNull()
            .WithMessage("Request không được để trống.");

        RuleFor(x => x.Request.Username)
            .NotEmpty()
            .WithMessage("Username không được để trống.");

        RuleFor(x => x.Request.Email)
            .NotEmpty()
            .WithMessage("Email không được để trống.")
            .EmailAddress()
            .WithMessage("Email không đúng định dạng.");

        RuleFor(x => x.Request.Password)
            .NotEmpty()
            .WithMessage("Mật khẩu không được để trống.")
            .MinimumLength(8)
            .WithMessage("Mật khẩu phải có ít nhất 8 ký tự.");
    }
}
