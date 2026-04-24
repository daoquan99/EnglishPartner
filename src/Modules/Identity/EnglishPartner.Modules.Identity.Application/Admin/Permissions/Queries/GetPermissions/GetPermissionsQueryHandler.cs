using EnglishPartner.Modules.Identity.Domain.Roles.Repositories;
using EnglishPartner.SharedKernel.CQRS;
using EnglishPartner.SharedKernel.Results;

namespace EnglishPartner.Modules.Identity.Application.Admin.Permissions.Queries.GetPermissions;

internal sealed class GetPermissionsQueryHandler(IPermissionRepository permissionRepository)
    : IQueryHandler<GetPermissionsQuery, List<PermissionItem>>
{
    public async Task<Result<List<PermissionItem>>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var permissions = await permissionRepository.GetAllAsync(cancellationToken);

        var response = permissions
            .Select(p => new PermissionItem(p.Id, p.Code, p.Name, p.Module, p.Description))
            .ToList();

        return Result.Success(response);
    }
}
