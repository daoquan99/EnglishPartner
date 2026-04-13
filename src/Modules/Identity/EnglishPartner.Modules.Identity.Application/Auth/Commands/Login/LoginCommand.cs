using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Identity.Application.Auth.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password) : ICommand<LoginResponse>;

public sealed record LoginResponse(
    Guid UserId,
    string DisplayName,
    string AccessToken,
    string RefreshToken,
    bool IsOnboardingCompleted);
