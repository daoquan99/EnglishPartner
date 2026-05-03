using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Identity.Application.Admin.Roles.Commands.AssignRolePermission;

public sealed record AssignRolePermissionCommand(Guid RoleId, Guid PermissionId) : ICommand;
