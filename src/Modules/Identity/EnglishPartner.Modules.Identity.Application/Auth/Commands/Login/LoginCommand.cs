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
    bool IsOnboardingCompleted,
    string[]? Permissions);  // null = Learner (omitted in JSON), non-null = admin-type user
                              // Roles: NOT returned — only in JWT for BE authorization
                              // RefreshToken: NOT in body — stored as httpOnly __Secure-ep-refresh cookie
