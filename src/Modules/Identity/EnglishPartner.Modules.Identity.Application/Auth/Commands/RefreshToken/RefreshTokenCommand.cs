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
    string[]? Permissions);  // null = Learner, non-null = admin-type
                              // RefreshToken: NOT returned — rotated via new httpOnly cookie set by LogoutCommand.cs
