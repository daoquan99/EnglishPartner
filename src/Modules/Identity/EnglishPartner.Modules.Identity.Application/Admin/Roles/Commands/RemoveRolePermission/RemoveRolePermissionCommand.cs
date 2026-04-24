using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Identity.Application.Admin.Roles.Commands.RemoveRolePermission;

public sealed record RemoveRolePermissionCommand(Guid RoleId, Guid PermissionId) : ICommand;
