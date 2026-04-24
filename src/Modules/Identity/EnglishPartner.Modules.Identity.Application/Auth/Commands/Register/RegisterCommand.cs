using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Identity.Application.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string DisplayName,
    int? UtcOffsetHours = null) : ICommand<RegisterResponse>;

public sealed record RegisterResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    string AccessToken,
    DateTime ExpiresAt,
    bool IsOnboardingCompleted);  // UTC. All DateTime values are UTC — client converts to local for display.

