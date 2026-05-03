using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Identity.Application.Auth.Commands.Logout;

public sealed record LogoutCommand(Guid UserId) : ICommand;
