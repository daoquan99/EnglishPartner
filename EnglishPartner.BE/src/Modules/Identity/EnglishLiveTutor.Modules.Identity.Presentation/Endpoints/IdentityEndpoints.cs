using System.Security.Claims;
using EnglishLiveTutor.Api.Common;
using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Identity.Commands.AdminCreateUser;
using EnglishLiveTutor.Application.Features.Identity.Commands.AdminUpdateUser;
using EnglishLiveTutor.Application.Features.Identity.Commands.AssignUserRoles;
using EnglishLiveTutor.Application.Features.Identity.Commands.CreatePermission;
using EnglishLiveTutor.Application.Features.Identity.Commands.CreateRole;
using EnglishLiveTutor.Application.Features.Identity.Commands.DeletePermission;
using EnglishLiveTutor.Application.Features.Identity.Commands.DeleteRole;
using EnglishLiveTutor.Application.Features.Identity.Commands.DeleteUser;
using EnglishLiveTutor.Application.Features.Identity.Commands.Login;
using EnglishLiveTutor.Application.Features.Identity.Commands.RefreshToken;
using EnglishLiveTutor.Application.Features.Identity.Commands.RegisterUser;
using EnglishLiveTutor.Application.Features.Identity.Commands.RevokeRefreshToken;
using EnglishLiveTutor.Application.Features.Identity.Commands.UpdatePermission;
using EnglishLiveTutor.Application.Features.Identity.Commands.UpdateRolePermissions;
using EnglishLiveTutor.Application.Features.Identity.Queries.GetUserProfile;
using EnglishLiveTutor.Application.Features.Identity.Queries.ListPermissions;
using EnglishLiveTutor.Application.Features.Identity.Queries.ListRoles;
using EnglishLiveTutor.Application.Features.Identity.Queries.ListUsers;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnglishLiveTutor.Api.Endpoints;

public static class IdentityEndpointRouteBuilderExtensions
{
    private const string RefreshCookieName = "elt_refresh_token";

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/api/auth");

        authGroup.MapPost("/register", async (
            HttpContext httpContext,
            RegisterUserRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            string? ip = httpContext.Connection.RemoteIpAddress?.ToString();
            var result = await sender.Send(new RegisterUserCommand(req, ip), ct);
            AppendRefreshTokenCookie(httpContext, result.RefreshToken, result.RefreshTokenExpiresAtUtc);
            return Results.Ok(result);
        }).RequireRateLimiting("auth").AllowAnonymous();

        authGroup.MapPost("/login", async (
            HttpContext httpContext,
            LoginRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            string? ip = httpContext.Connection.RemoteIpAddress?.ToString();
            var result = await sender.Send(new LoginCommand(req, ip), ct);
            AppendRefreshTokenCookie(httpContext, result.RefreshToken, result.RefreshTokenExpiresAtUtc);
            return Results.Ok(result);
        }).RequireRateLimiting("auth").AllowAnonymous();

        authGroup.MapPost("/refresh", async (
            HttpContext httpContext,
            RefreshTokenRequest? req,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                string? rawToken = !string.IsNullOrWhiteSpace(req?.RefreshToken)
                    ? req.RefreshToken
                    : httpContext.Request.Cookies[RefreshCookieName];

                if (string.IsNullOrWhiteSpace(rawToken))
                {
                    return ApiProblem.Unauthorized("Thiếu refresh token.", httpContext);
                }

                string? ip = httpContext.Connection.RemoteIpAddress?.ToString();
                var effectiveReq = new RefreshTokenRequest(rawToken, req?.DeviceInfo ?? "Browser");
                var result = await sender.Send(new RefreshTokenCommand(effectiveReq, ip), ct);
                AppendRefreshTokenCookie(httpContext, result.RefreshToken, result.RefreshTokenExpiresAtUtc);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                DeleteRefreshTokenCookie(httpContext);
                return ApiProblem.Unauthorized(ex.Message, httpContext);
            }
        }).RequireRateLimiting("auth-refresh").AllowAnonymous();

        authGroup.MapPost("/revoke", async (
            HttpContext httpContext,
            RevokeTokenRequest? req,
            ISender sender,
            CancellationToken ct) =>
        {
            string? rawToken = !string.IsNullOrWhiteSpace(req?.RefreshToken)
                ? req.RefreshToken
                : httpContext.Request.Cookies[RefreshCookieName];

            DeleteRefreshTokenCookie(httpContext);

            if (string.IsNullOrWhiteSpace(rawToken))
            {
                return Results.Ok(new { revoked = true });
            }

            bool revoked = await sender.Send(
                new RevokeRefreshTokenCommand(new RevokeTokenRequest(rawToken, req?.Reason ?? "User logout")),
                ct);
            return Results.Ok(new { revoked });
        }).AllowAnonymous();

        authGroup.MapGet("/me", async (
            HttpContext httpContext,
            ISender sender,
            CancellationToken ct) =>
        {
            if (httpContext.User?.Identity?.IsAuthenticated != true)
            {
                return ApiProblem.Unauthorized();
            }

            string? sub = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContext.User.FindFirstValue("sub");

            if (!Guid.TryParse(sub, out var userId))
            {
                return ApiProblem.Unauthorized();
            }

            var profile = await sender.Send(new GetUserProfileQuery(userId), ct);
            return profile is null ? ApiProblem.NotFound("Không tìm thấy người dùng.") : Results.Ok(profile);
        }).RequireAuthorization();

        // Users Management (Admin RBAC Protected)
        var usersGroup = app.MapGroup("/api/users").RequireAuthorization();

        usersGroup.MapGet("/", async (
            HttpContext httpContext,
            string? search,
            int? page,
            int? pageSize,
            bool? paged,
            ISender sender,
            CancellationToken ct) =>
        {
            var pagedUsers = await sender.Send(new ListUsersQuery(search, page, pageSize), ct);
            return pagedUsers.ToHttpResult(httpContext, paged);
        }).RequirePermission("users.read");

        usersGroup.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var profile = await sender.Send(new GetUserProfileQuery(id), ct);
            return profile is null ? ApiProblem.NotFound("Không tìm thấy người dùng.") : Results.Ok(profile);
        }).RequirePermission("users.read");

        usersGroup.MapPut("/{id:guid}/roles", async (
            Guid id,
            AssignUserRolesRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var updated = await sender.Send(new AssignUserRolesCommand(id, req), ct);
            return Results.Ok(updated);
        }).RequirePermission("users.manage");

        usersGroup.MapPost("/", async (
            AdminCreateUserRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var created = await sender.Send(new AdminCreateUserCommand(req), ct);
            return Results.Ok(created);
        }).RequirePermission("users.manage");

        usersGroup.MapPut("/{id:guid}", async (
            Guid id,
            AdminUpdateUserRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var updated = await sender.Send(new AdminUpdateUserCommand(id, req), ct);
            return updated is null
                ? ApiProblem.NotFound("Không tìm thấy người dùng.")
                : Results.Ok(updated);
        }).RequirePermission("users.manage");

        usersGroup.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            bool deleted = await sender.Send(new DeleteUserCommand(id), ct);
            return deleted ? Results.NoContent() : ApiProblem.NotFound("Không tìm thấy người dùng.");
        }).RequirePermission("users.manage");

        // Roles Management
        var rolesGroup = app.MapGroup("/api/roles").RequireAuthorization();

        rolesGroup.MapGet("/", async (
            HttpContext httpContext,
            string? search,
            int? page,
            int? pageSize,
            bool? paged,
            ISender sender,
            CancellationToken ct) =>
        {
            var pagedRoles = await sender.Send(new ListRolesQuery(search, page, pageSize), ct);
            return pagedRoles.ToHttpResult(httpContext, paged);
        }).RequirePermission("roles.manage");

        rolesGroup.MapPost("/", async (
            CreateRoleRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var created = await sender.Send(new CreateRoleCommand(req), ct);
            return Results.Ok(created);
        }).RequirePermission("roles.manage");

        rolesGroup.MapPut("/{id:guid}/permissions", async (
            Guid id,
            UpdateRolePermissionsRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var updated = await sender.Send(new UpdateRolePermissionsCommand(id, req), ct);
            return Results.Ok(updated);
        }).RequirePermission("roles.manage");

        rolesGroup.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            bool deleted = await sender.Send(new DeleteRoleCommand(id), ct);
            return deleted ? Results.NoContent() : ApiProblem.NotFound("Không tìm thấy vai trò.");
        }).RequirePermission("roles.manage");

        // Permissions Management
        var permissionsGroup = app.MapGroup("/api/permissions").RequireAuthorization();

        permissionsGroup.MapGet("/", async (
            HttpContext httpContext,
            string? search,
            int? page,
            int? pageSize,
            bool? paged,
            ISender sender,
            CancellationToken ct) =>
        {
            var pagedPerms = await sender.Send(new ListPermissionsQuery(search, page, pageSize), ct);
            return pagedPerms.ToHttpResult(httpContext, paged);
        }).RequirePermission("roles.manage");

        permissionsGroup.MapPost("/", async (
            CreatePermissionRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var created = await sender.Send(new CreatePermissionCommand(req), ct);
            return Results.Ok(created);
        }).RequirePermission("roles.manage");

        permissionsGroup.MapPut("/{id:guid}", async (
            Guid id,
            AdminUpdatePermissionRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var updated = await sender.Send(new UpdatePermissionCommand(id, req), ct);
            return updated is null ? ApiProblem.NotFound("Không tìm thấy quyền.") : Results.Ok(updated);
        }).RequirePermission("roles.manage");

        permissionsGroup.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            bool deleted = await sender.Send(new DeletePermissionCommand(id), ct);
            return deleted ? Results.NoContent() : ApiProblem.NotFound("Không tìm thấy quyền.");
        }).RequirePermission("roles.manage");

        return app;
    }

    private static void AppendRefreshTokenCookie(HttpContext httpContext, string refreshToken, DateTimeOffset expiresAtUtc)
    {
        httpContext.Response.Cookies.Append(RefreshCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = expiresAtUtc,
            Path = "/api/auth"
        });
    }

    private static void DeleteRefreshTokenCookie(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth"
        });
    }
}
