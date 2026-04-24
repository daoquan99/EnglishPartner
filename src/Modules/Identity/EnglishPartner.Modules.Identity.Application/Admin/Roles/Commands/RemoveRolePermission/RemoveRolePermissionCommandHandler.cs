using EnglishPartner.Modules.Identity.Domain;
using EnglishPartner.Modules.Identity.Domain.Roles.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Identity.Application.Admin.Roles.Commands.RemoveRolePermission;

internal sealed class RemoveRolePermissionCommandHandler(IRoleRepository roleRepository)
    : ICommandHandler<RemoveRolePermissionCommand>
{
    public async Task<Result> Handle(RemoveRolePermissionCommand request, CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
            return Result.Failure(IdentityErrors.Role.NotFound);

        if (role.RolePermissions.All(rp => rp.PermissionId != request.PermissionId))
            return Result.Failure(IdentityErrors.Role.PermissionNotAssigned);

        role.RemovePermission(request.PermissionId);

        return Result.Success();
    }
}
