using EnglishPartner.SharedKernel.CQRS;

namespace EnglishPartner.Modules.Identity.Application.Admin.Roles.Queries.GetRoles;

public sealed record GetRolesQuery : IQuery<List<RoleResponse>>;

public sealed record RoleResponse(
    Guid Id,
    string Name,
    string? Description,
    List<PermissionResponse> Permissions);

public sealed record PermissionResponse(
    Guid Id,
    string Code,
    string Name,
    string Module,
    string? Description);
