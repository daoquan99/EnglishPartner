using EnglishPartner.Modules.Identity.Application.Auth.Commands.ChangePassword;
using EnglishPartner.Modules.Identity.Application.Auth.Commands.Login;
using EnglishPartner.Modules.Identity.Application.Auth.Commands.Logout;
using EnglishPartner.Modules.Identity.Application.Auth.Commands.RefreshToken;
using EnglishPartner.Modules.Identity.Application.Auth.Commands.Register;
using EnglishPartner.Modules.Identity.Application.Auth.Commands.SocialLogin;
using EnglishPartner.SharedKernel.Extensions;
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
                : result.ToProblemDetails();  // QUAL-3 fix: consistent error format
        })
        .WithName("Register")
        .WithSummary("Register a new user account.")
        .AllowAnonymous();

        group.MapPost("/login", async (LoginRequest req, ISender sender) =>
        {
            var result = await sender.Send(new LoginCommand(req.Email, req.Password));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.Unauthorized();
        })
        .WithName("Login")
        .WithSummary("Authenticate with email and password. Returns JWT access + refresh tokens.")
        .AllowAnonymous();

        group.MapPost("/social-login", async (SocialLoginRequest req, ISender sender) =>
        {
            var result = await sender.Send(new SocialLoginCommand(req.Provider, req.Token));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("SocialLogin")
        .WithSummary("Authenticate with a social provider (e.g. Google). Returns JWT access + refresh tokens.")
        .AllowAnonymous();

        group.MapPost("/refresh", async (RefreshRequest req, ISender sender) =>
        {
            var result = await sender.Send(new RefreshTokenCommand(req.RefreshToken));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.Unauthorized();
        })
        .WithName("RefreshToken")
        .WithSummary("Exchange a valid refresh token for a new JWT access token.")
        .AllowAnonymous();

        group.MapPost("/logout", async (ClaimsPrincipal user, ISender sender) =>
        {
            if (!user.TryGetUserId(out var userId)) return Results.Unauthorized();
            var result = await sender.Send(new LogoutCommand(userId));
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .WithName("Logout")
        .WithSummary("Invalidate the current user's refresh token.")
        .RequireAuthorization();

        group.MapPost("/change-password", async (ChangePasswordRequest req, ClaimsPrincipal user, ISender sender) =>
        {
            if (!user.TryGetUserId(out var userId)) return Results.Unauthorized();
            var result = await sender.Send(new ChangePasswordCommand(userId, req.CurrentPassword, req.NewPassword));
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .WithName("ChangePassword")
        .WithSummary("Change the current user's password.")
        .RequireAuthorization();
    }
}

internal sealed record RegisterRequest(string Email, string Password, string DisplayName);
internal sealed record LoginRequest(string Email, string Password);
internal sealed record SocialLoginRequest(string Provider, string Token);
internal sealed record RefreshRequest(string RefreshToken);
internal sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
