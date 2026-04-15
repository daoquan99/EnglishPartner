using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Identity.Application.Admin.Users.Commands.DeleteUser;

public sealed record DeleteUserCommand(Guid UserId) : ICommand;
