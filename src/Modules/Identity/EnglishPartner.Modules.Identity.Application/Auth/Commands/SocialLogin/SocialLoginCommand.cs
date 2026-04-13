using EnglishPartner.Modules.Identity.Application.Auth.Commands.Login;
using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Identity.Application.Auth.Commands.SocialLogin;

public sealed record SocialLoginCommand(
    string Provider,
    string Token) : ICommand<LoginResponse>;
