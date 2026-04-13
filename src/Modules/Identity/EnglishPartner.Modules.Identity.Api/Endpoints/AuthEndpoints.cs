using EnglishPartner.Modules.Identity.Application.Auth.Commands.ChangePassword;
using EnglishPartner.Modules.Identity.Application.Auth.Commands.Login;
using EnglishPartner.Modules.Identity.Application.Auth.Commands.Logout;
using EnglishPartner.Modules.Identity.Application.Auth.Commands.RefreshToken;
using EnglishPartner.Modules.Identity.Application.Auth.Commands.Register;
using EnglishPartner.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace EnglishPartner.Modules.Identity.Api.Endpoints;

internal static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterRequest req, ISender sender) =>
        {
            var result = await sender.Send(new RegisterCommand(req.Email, req.Password, req.DisplayName));
            return result.IsSuccess
                ? Results.Created($"/api/v1/auth/me", result.Value)
                : Results.BadRequest(result.Error);
        }).AllowAnonymous();

        group.MapPost("/login", async (LoginRequest req, ISender sender) =>
        {
            var result = await sender.Send(new LoginCommand(req.Email, req.Password));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Unauthorized();
        }).AllowAnonymous();

        group.MapPost("/refresh", async (RefreshRequest req, ISender sender) =>
        {
            var result = await sender.Send(new RefreshTokenCommand(req.RefreshToken));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Unauthorized();
        }).AllowAnonymous();

        group.MapPost("/logout", async (ClaimsPrincipal user, ISender sender) =>
        {
            var userId = GetUserId(user);
            if (userId is null) return Results.Unauthorized();

            var result = await sender.Send(new LogoutCommand(userId.Value));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        }).RequireAuthorization();

        group.MapPost("/change-password", async (ChangePasswordRequest req, ClaimsPrincipal user, ISender sender) =>
        {
            var userId = GetUserId(user);
            if (userId is null) return Results.Unauthorized();

            var result = await sender.Send(new ChangePasswordCommand(userId.Value, req.CurrentPassword, req.NewPassword));
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        }).RequireAuthorization();
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

internal sealed record RegisterRequest(string Email, string Password, string DisplayName);
internal sealed record LoginRequest(string Email, string Password);
internal sealed record RefreshRequest(string RefreshToken);
internal sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
