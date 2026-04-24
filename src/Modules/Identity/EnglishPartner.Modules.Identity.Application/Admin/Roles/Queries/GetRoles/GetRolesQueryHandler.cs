using EnglishPartner.Modules.Identity.Domain.Roles.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Identity.Application.Admin.Roles.Queries.GetRoles;

internal sealed class GetRolesQueryHandler(IRoleRepository roleRepository)
    : IQueryHandler<GetRolesQuery, List<RoleResponse>>
{
    public async Task<Result<List<RoleResponse>>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await roleRepository.GetAllWithPermissionsAsync(cancellationToken);

        var response = roles.Select(r => new RoleResponse(
            r.Id,
            r.Name,
            r.Description,
            r.RolePermissions
                .Where(rp => rp.Permission is not null)
                .Select(rp => new PermissionResponse(
                    rp.Permission!.Id,
                    rp.Permission.Code,
                    rp.Permission.Name,
                    rp.Permission.Module,
                    rp.Permission.Description))
                .ToList()
        )).ToList();

        return Result.Success(response);
    }
}
