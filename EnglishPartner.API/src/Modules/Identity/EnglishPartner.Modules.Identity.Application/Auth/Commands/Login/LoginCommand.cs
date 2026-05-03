using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Identity.Application.Auth.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password,
    int? UtcOffsetHours = null) : ICommand<LoginResponse>;

public sealed record LoginResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    string AccessToken,
    DateTime ExpiresAt,     // UTC. Client converts to local for display.
    bool IsOnboardingCompleted,
    string[]? Permissions);  // null = Learner (omitted in JSON), non-null = admin-type user
