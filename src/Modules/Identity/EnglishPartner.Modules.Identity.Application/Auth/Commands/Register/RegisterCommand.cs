using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Identity.Application.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string DisplayName) : ICommand<RegisterResponse>;

public sealed record RegisterResponse(
    Guid UserId,
    string AccessToken,
    string RefreshToken);
