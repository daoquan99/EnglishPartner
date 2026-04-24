using EnglishPartner.Modules.Identity.Domain;
using EnglishPartner.Modules.Identity.Domain.Roles.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Identity.Application.Admin.Roles.Commands.AssignRolePermission;

internal sealed class AssignRolePermissionCommandHandler(
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository)
    : ICommandHandler<AssignRolePermissionCommand>
{
    public async Task<Result> Handle(AssignRolePermissionCommand request, CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
            return Result.Failure(IdentityErrors.Role.NotFound);

        var permission = await permissionRepository.GetByIdAsync(request.PermissionId, cancellationToken);
        if (permission is null)
            return Result.Failure(IdentityErrors.Permission.NotFoundById(request.PermissionId));

        if (role.RolePermissions.Any(rp => rp.PermissionId == request.PermissionId))
            return Result.Failure(IdentityErrors.Role.PermissionAlreadyAssigned);

        role.AssignPermission(request.PermissionId);

        return Result.Success();
    }
}
