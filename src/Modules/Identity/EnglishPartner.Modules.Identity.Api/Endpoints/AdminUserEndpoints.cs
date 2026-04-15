using EnglishPartner.Modules.Identity.Application.Admin.Users.Commands.ActivateUser;
using EnglishPartner.Modules.Identity.Application.Admin.Users.Commands.DeactivateUser;
using EnglishPartner.Modules.Identity.Application.Admin.Users.Commands.DeleteUser;
using EnglishPartner.Modules.Identity.Application.Admin.Users.Commands.ManageUserRole;
using EnglishPartner.Modules.Identity.Application.Admin.Users.Commands.ResetUserPassword;
using EnglishPartner.Modules.Identity.Application.Admin.Users.Queries.GetUsers;
using EnglishPartner.Modules.Identity.Domain.Roles;
using EnglishPartner.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnglishPartner.Modules.Identity.Api.Endpoints;

/// <summary>
/// Admin endpoints for user management.
/// All require Admin role + specific permissions.
/// </summary>
public static class AdminUserEndpoints
{
    public static void MapAdminUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/admin/identity")
            .RequireAuthorization("Admin")
            .WithTags("Admin — Identity");

        // GET /api/v1/admin/identity/users
        group.MapGet("/users", async (
            ISender sender,
            string? search       = null,
            string? role         = null,
            bool?   isActive     = null,
            int     page         = 1,
            int     pageSize     = 20,
            CancellationToken cancellationToken = default) =>
        {
            var result = await sender.Send(
                new GetUsersQuery(search, role, isActive, page, pageSize), cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .RequireAuthorization(Permissions.Identity.UsersManage);

        // POST /api/v1/admin/identity/users/{id}/activate
        group.MapPost("/users/{id:guid}/activate", async (
            Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ActivateUserCommand(id), cancellationToken);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .RequireAuthorization(Permissions.Identity.UsersManage);

        // POST /api/v1/admin/identity/users/{id}/deactivate
        group.MapPost("/users/{id:guid}/deactivate", async (
            Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeactivateUserCommand(id), cancellationToken);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .RequireAuthorization(Permissions.Identity.UsersManage);

        // DELETE /api/v1/admin/identity/users/{id}
        group.MapDelete("/users/{id:guid}", async (
            Guid id, ISender sender, CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new DeleteUserCommand(id), cancellationToken);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .RequireAuthorization(Permissions.Identity.UsersManage);

        // POST /api/v1/admin/identity/users/{id}/reset-password
        group.MapPost("/users/{id:guid}/reset-password", async (
            Guid id,
            ResetPasswordRequest body,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(new ResetUserPasswordCommand(id, body.SendEmail), cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .RequireAuthorization(Permissions.Identity.UsersManage);

        // POST /api/v1/admin/identity/users/{id}/roles
        group.MapPost("/users/{id:guid}/roles", async (
            Guid id,
            ManageRoleRequest body,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var result = await sender.Send(
                new ManageUserRoleCommand(id, body.RoleId, body.Operation), cancellationToken);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .RequireAuthorization(Permissions.Identity.RolesManage);
    }

    private sealed record ResetPasswordRequest(bool SendEmail);
    private sealed record ManageRoleRequest(Guid RoleId, string Operation);
}
