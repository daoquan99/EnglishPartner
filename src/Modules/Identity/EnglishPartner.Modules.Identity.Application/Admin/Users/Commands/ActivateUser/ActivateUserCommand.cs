using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Identity.Application.Admin.Users.Commands.ActivateUser;

public sealed record ActivateUserCommand(Guid UserId) : ICommand;
