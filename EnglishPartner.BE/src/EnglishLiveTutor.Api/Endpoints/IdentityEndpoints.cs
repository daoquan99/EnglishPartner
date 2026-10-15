using System.Security.Claims;
using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Identity.Commands.AssignUserRoles;
using EnglishLiveTutor.Application.Features.Identity.Commands.CreatePermission;
using EnglishLiveTutor.Application.Features.Identity.Commands.CreateRole;
using EnglishLiveTutor.Application.Features.Identity.Commands.Login;
using EnglishLiveTutor.Application.Features.Identity.Commands.RefreshToken;
using EnglishLiveTutor.Application.Features.Identity.Commands.RegisterUser;
using EnglishLiveTutor.Application.Features.Identity.Commands.RevokeRefreshToken;
using EnglishLiveTutor.Application.Features.Identity.Commands.UpdateRolePermissions;
using EnglishLiveTutor.Application.Features.Identity.Queries.GetUserProfile;
using EnglishLiveTutor.Application.Features.Identity.Queries.ListPermissions;
using EnglishLiveTutor.Application.Features.Identity.Queries.ListRoles;
using EnglishLiveTutor.Application.Features.Identity.Queries.ListUsers;

namespace EnglishLiveTutor.Api.Endpoints;

public static class IdentityEndpoints
{
    private const string RefreshCookieName = "elt_refresh_token";

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        var authGroup = app.MapGroup("/api/auth");

        authGroup.MapPost("/register", async (
            HttpContext httpContext,
            RegisterUserRequest req,
            ICommandHandler<RegisterUserCommand, AuthResponseDto> handler,
            CancellationToken ct) =>
        {
            try
            {
                string? ip = httpContext.Connection.RemoteIpAddress?.ToString();
                var result = await handler.HandleAsync(new RegisterUserCommand(req, ip), ct);
                AppendRefreshTokenCookie(httpContext, result.RefreshToken, result.RefreshTokenExpiresAtUtc);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        authGroup.MapPost("/login", async (
            HttpContext httpContext,
            LoginRequest req,
            ICommandHandler<LoginCommand, AuthResponseDto> handler,
            CancellationToken ct) =>
        {
            try
            {
                string? ip = httpContext.Connection.RemoteIpAddress?.ToString();
                var result = await handler.HandleAsync(new LoginCommand(req, ip), ct);
                AppendRefreshTokenCookie(httpContext, result.RefreshToken, result.RefreshTokenExpiresAtUtc);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        authGroup.MapPost("/refresh", async (
            HttpContext httpContext,
            RefreshTokenRequest? req,
            ICommandHandler<RefreshTokenCommand, AuthResponseDto> handler,
            CancellationToken ct) =>
        {
            try
            {
                string? rawToken = !string.IsNullOrWhiteSpace(req?.RefreshToken)
                    ? req.RefreshToken
                    : httpContext.Request.Cookies[RefreshCookieName];

                if (string.IsNullOrWhiteSpace(rawToken))
                {
                    return Results.Unauthorized();
                }

                string? ip = httpContext.Connection.RemoteIpAddress?.ToString();
                var effectiveReq = new RefreshTokenRequest(rawToken, req?.DeviceInfo ?? "Browser");
                var result = await handler.HandleAsync(new RefreshTokenCommand(effectiveReq, ip), ct);
                AppendRefreshTokenCookie(httpContext, result.RefreshToken, result.RefreshTokenExpiresAtUtc);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                DeleteRefreshTokenCookie(httpContext);
                return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status401Unauthorized);
            }
        });

        authGroup.MapPost("/revoke", async (
            HttpContext httpContext,
            RevokeTokenRequest? req,
            ICommandHandler<RevokeRefreshTokenCommand, bool> handler,
            CancellationToken ct) =>
        {
            try
            {
                string? rawToken = !string.IsNullOrWhiteSpace(req?.RefreshToken)
                    ? req.RefreshToken
                    : httpContext.Request.Cookies[RefreshCookieName];

                DeleteRefreshTokenCookie(httpContext);

                if (string.IsNullOrWhiteSpace(rawToken))
                {
                    return Results.Ok(new { revoked = true });
                }

                bool revoked = await handler.HandleAsync(
                    new RevokeRefreshTokenCommand(new RevokeTokenRequest(rawToken, req?.Reason ?? "User logout")),
                    ct);
                return Results.Ok(new { revoked });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        authGroup.MapGet("/me", async (
            HttpContext httpContext,
            IQueryHandler<GetUserProfileQuery, UserProfileDto?> handler,
            CancellationToken ct) =>
        {
            if (httpContext.User?.Identity?.IsAuthenticated != true)
            {
                return Results.Unauthorized();
            }

            string? sub = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? httpContext.User.FindFirstValue("sub");

            if (!Guid.TryParse(sub, out var userId))
            {
                return Results.Unauthorized();
            }

            var profile = await handler.HandleAsync(new GetUserProfileQuery(userId), ct);
            return profile is null ? Results.NotFound() : Results.Ok(profile);
        });

        // Users Management (Admin RBAC Protected)
        var usersGroup = app.MapGroup("/api/users");

        usersGroup.MapGet("/", async (
            IQueryHandler<ListUsersQuery, List<UserProfileDto>> handler,
            CancellationToken ct) =>
        {
            var users = await handler.HandleAsync(new ListUsersQuery(), ct);
            return Results.Ok(users);
        }).RequirePermission("users.read");

        usersGroup.MapGet("/{id:guid}", async (
            Guid id,
            IQueryHandler<GetUserProfileQuery, UserProfileDto?> handler,
            CancellationToken ct) =>
        {
            var profile = await handler.HandleAsync(new GetUserProfileQuery(id), ct);
            return profile is null ? Results.NotFound() : Results.Ok(profile);
        }).RequirePermission("users.read");

        usersGroup.MapPut("/{id:guid}/roles", async (
            Guid id,
            AssignUserRolesRequest req,
            ICommandHandler<AssignUserRolesCommand, UserProfileDto> handler,
            CancellationToken ct) =>
        {
            try
            {
                var updated = await handler.HandleAsync(new AssignUserRolesCommand(id, req), ct);
                return Results.Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("users.manage");

        // Roles Management
        var rolesGroup = app.MapGroup("/api/roles");

        rolesGroup.MapGet("/", async (
            IQueryHandler<ListRolesQuery, List<RoleDto>> handler,
            CancellationToken ct) =>
        {
            var roles = await handler.HandleAsync(new ListRolesQuery(), ct);
            return Results.Ok(roles);
        }).RequirePermission("roles.manage");

        rolesGroup.MapPost("/", async (
            CreateRoleRequest req,
            ICommandHandler<CreateRoleCommand, RoleDto> handler,
            CancellationToken ct) =>
        {
            try
            {
                var created = await handler.HandleAsync(new CreateRoleCommand(req), ct);
                return Results.Ok(created);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("roles.manage");

        rolesGroup.MapPut("/{id:guid}/permissions", async (
            Guid id,
            UpdateRolePermissionsRequest req,
            ICommandHandler<UpdateRolePermissionsCommand, RoleDto> handler,
            CancellationToken ct) =>
        {
            try
            {
                var updated = await handler.HandleAsync(new UpdateRolePermissionsCommand(id, req), ct);
                return Results.Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("roles.manage");

        // Permissions Management
        var permissionsGroup = app.MapGroup("/api/permissions");

        permissionsGroup.MapGet("/", async (
            IQueryHandler<ListPermissionsQuery, List<PermissionDto>> handler,
            CancellationToken ct) =>
        {
            var perms = await handler.HandleAsync(new ListPermissionsQuery(), ct);
            return Results.Ok(perms);
        }).RequirePermission("roles.manage");

        permissionsGroup.MapPost("/", async (
            CreatePermissionRequest req,
            ICommandHandler<CreatePermissionCommand, PermissionDto> handler,
            CancellationToken ct) =>
        {
            try
            {
                var created = await handler.HandleAsync(new CreatePermissionCommand(req), ct);
                return Results.Ok(created);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("roles.manage");

        return app;
    }

    private static void AppendRefreshTokenCookie(HttpContext httpContext, string refreshToken, DateTimeOffset expiresAtUtc)
    {
        httpContext.Response.Cookies.Append(RefreshCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = httpContext.Request.IsHttps,
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
            Secure = httpContext.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth"
        });
    }
}
