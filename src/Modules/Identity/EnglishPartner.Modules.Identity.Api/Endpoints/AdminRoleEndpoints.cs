using EnglishPartner.Modules.Identity.Application.Admin.Roles.Commands.AssignRolePermission;
using EnglishPartner.Modules.Identity.Application.Admin.Roles.Commands.CreateRole;
using EnglishPartner.Modules.Identity.Application.Admin.Roles.Commands.DeleteRole;
using EnglishPartner.Modules.Identity.Application.Admin.Roles.Commands.RemoveRolePermission;
using EnglishPartner.Modules.Identity.Application.Admin.Roles.Commands.UpdateRole;
using EnglishPartner.Modules.Identity.Application.Admin.Roles.Queries.GetRoles;
using EnglishPartner.Modules.Identity.Application.Admin.Permissions.Queries.GetPermissions;
using EnglishPartner.Modules.Identity.Domain.Roles;
using EnglishPartner.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnglishPartner.Modules.Identity.Api.Endpoints;

/// <summary>
/// Admin endpoints for Role and Permission management.
/// All require Admin role + identity.roles.manage permission.
/// </summary>
public static class AdminRoleEndpoints
{
    public static void MapAdminRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/admin/identity")
            .RequireAuthorization("Admin")
            .RequireAuthorization(Permissions.Identity.RolesManage)
            .WithTags("Admin — Roles & Permissions");

        // GET /api/v1/admin/identity/roles
        group.MapGet("/roles", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetRolesQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetRoles")
        .WithSummary("Get all roles with their permissions.");

        // POST /api/v1/admin/identity/roles
        group.MapPost("/roles", async (CreateRoleRequest body, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateRoleCommand(body.Name, body.Description), ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/admin/identity/roles/{result.Value!.Id}", result.Value)
                : result.ToProblemDetails();
        })
        .WithName("CreateRole")
        .WithSummary("Create a new role.");

        // PUT /api/v1/admin/identity/roles/{id}
        group.MapPut("/roles/{id:guid}", async (Guid id, UpdateRoleRequest body, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateRoleCommand(id, body.Name, body.Description), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .WithName("UpdateRole")
        .WithSummary("Update a role's name and description.");

        // DELETE /api/v1/admin/identity/roles/{id}
        group.MapDelete("/roles/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteRoleCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .WithName("DeleteRole")
        .WithSummary("Delete a role. Fails if the role is still assigned to any user.");

        // POST /api/v1/admin/identity/roles/{id}/permissions
        group.MapPost("/roles/{id:guid}/permissions", async (
            Guid id, AssignPermissionRequest body, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new AssignRolePermissionCommand(id, body.PermissionId), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .WithName("AssignRolePermission")
        .WithSummary("Assign a permission to a role.");

        // DELETE /api/v1/admin/identity/roles/{id}/permissions/{permissionId}
        group.MapDelete("/roles/{id:guid}/permissions/{permissionId:guid}", async (
            Guid id, Guid permissionId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RemoveRolePermissionCommand(id, permissionId), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .WithName("RemoveRolePermission")
        .WithSummary("Remove a permission from a role.");

        // GET /api/v1/admin/identity/permissions
        group.MapGet("/permissions", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPermissionsQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetPermissions")
        .WithSummary("List all available system permissions. Read-only.");
    }

    private sealed record CreateRoleRequest(string Name, string? Description);
    private sealed record UpdateRoleRequest(string Name, string? Description);
    private sealed record AssignPermissionRequest(Guid PermissionId);
}
