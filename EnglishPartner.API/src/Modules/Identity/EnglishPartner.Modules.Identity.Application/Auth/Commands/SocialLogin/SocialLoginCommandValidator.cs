using FluentValidation;

namespace EnglishPartner.Modules.Identity.Application.Auth.Commands.SocialLogin;

internal sealed class SocialLoginCommandValidator : AbstractValidator<SocialLoginCommand>
{
    private static readonly HashSet<string> _supportedProviders =
        ["Google", "Facebook", "Apple"];

    public SocialLoginCommandValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty()
            .Must(p => _supportedProviders.Contains(p))
            .WithMessage("Provider must be one of: Google, Facebook, Apple.");

        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("Token is required.");
    }
}
