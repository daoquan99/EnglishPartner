using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Identity.Application.Admin.Roles.Commands.DeleteRole;

public sealed record DeleteRoleCommand(Guid RoleId) : ICommand;
