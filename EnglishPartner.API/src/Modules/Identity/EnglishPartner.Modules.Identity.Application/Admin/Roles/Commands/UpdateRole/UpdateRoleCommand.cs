using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Identity.Application.Admin.Roles.Commands.UpdateRole;

public sealed record UpdateRoleCommand(Guid RoleId, string Name, string? Description) : ICommand;
