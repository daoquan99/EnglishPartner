using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.DeleteUser;

public sealed class DeleteUserValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("UserId không được để trống.");
    }
}
