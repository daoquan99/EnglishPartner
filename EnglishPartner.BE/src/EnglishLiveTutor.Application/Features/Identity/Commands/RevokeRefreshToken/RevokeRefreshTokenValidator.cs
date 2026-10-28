using FluentValidation;

namespace EnglishLiveTutor.Application.Features.Identity.Commands.RevokeRefreshToken;

public sealed class RevokeRefreshTokenValidator : AbstractValidator<RevokeRefreshTokenCommand>
{
    public RevokeRefreshTokenValidator()
    {
        RuleFor(x => x.Request).NotNull().WithMessage("Request không được để trống.");
    }
}
