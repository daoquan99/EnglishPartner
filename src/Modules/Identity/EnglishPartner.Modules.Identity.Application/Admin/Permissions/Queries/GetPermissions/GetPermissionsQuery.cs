using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Identity.Application.Admin.Permissions.Queries.GetPermissions;

public sealed record GetPermissionsQuery : IQuery<List<PermissionItem>>;

public sealed record PermissionItem(
    Guid Id,
    string Code,
    string Name,
    string Module,
    string? Description);
