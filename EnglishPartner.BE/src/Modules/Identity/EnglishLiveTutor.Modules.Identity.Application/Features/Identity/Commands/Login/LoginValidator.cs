using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.Login;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");
    }
}
