using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Identity.Application.Auth.Commands.RefreshToken;

/// <summary>
/// The refresh token value is read from the __Secure-ep-refresh httpOnly cookie by the endpoint,
/// then passed here. It is NOT read from the request body.
/// </summary>
public sealed record RefreshTokenCommand(string RefreshToken) : ICommand<RefreshTokenResponse>;

public sealed record RefreshTokenResponse(
    string AccessToken,
    DateTime ExpiresAt,      // UTC. Client converts to local for display.
    string[]? Permissions);  // null = Learner, non-null = admin-type
