using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.RegisterUser;

public sealed class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");
    }
}
