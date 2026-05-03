using EnglishPartner.Modules.Identity.Application.Auth.Commands.ChangePassword;
using EnglishPartner.Modules.Identity.Application.Auth.Commands.ForgotPassword;
using EnglishPartner.Modules.Identity.Application.Auth.Commands.Login;
using EnglishPartner.Modules.Identity.Application.Auth.Commands.Logout;
using EnglishPartner.Modules.Identity.Application.Auth.Commands.RefreshToken;
using EnglishPartner.Modules.Identity.Application.Auth.Commands.Register;
using EnglishPartner.Modules.Identity.Application.Auth.Commands.ResetPassword;
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
    /// <summary>Parses X-Timezone-Offset header (int, UTC offset hours). Returns null if absent or invalid.</summary>
    private static int? ParseOffset(HttpRequest req)
    {
        if (req.Headers.TryGetValue("X-Timezone-Offset", out var raw)
            && int.TryParse(raw, out var offset)
            && offset is >= -12 and <= 14)
            return offset;
        return null;
    }

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterRequest req, HttpRequest httpReq, ISender sender) =>
        {
            var utcOffset = ParseOffset(httpReq);
            var result = await sender.Send(new RegisterCommand(req.Email, req.Password, req.DisplayName, utcOffset));
            return result.IsSuccess
                ? Results.Created($"/api/v1/auth/me", result.Value)
                : result.ToProblemDetails();
        })
        .WithName("Register")
        .WithSummary("Register a new user account.")
        .AllowAnonymous();

        group.MapPost("/login", async (LoginRequest req, HttpRequest httpReq, ISender sender) =>
        {
            var utcOffset = ParseOffset(httpReq);
            var result = await sender.Send(new LoginCommand(req.Email, req.Password, utcOffset));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("Login")
        .WithSummary("Authenticate with email and password. Sets httpOnly refresh token cookie.")
        .AllowAnonymous();

        group.MapPost("/social-login", async (SocialLoginRequest req, ISender sender) =>
        {
            var result = await sender.Send(new SocialLoginCommand(req.Provider, req.Token));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("SocialLogin")
        .WithSummary("Authenticate with a social provider (e.g. Google). Sets httpOnly refresh token cookie.")
        .AllowAnonymous();

        group.MapPost("/refresh", async (HttpContext ctx, ISender sender) =>
        {
            var refreshToken = ctx.Request.Cookies["__Secure-ep-refresh"];
            if (string.IsNullOrEmpty(refreshToken))
                return Results.Unauthorized();
            var result = await sender.Send(new RefreshTokenCommand(refreshToken));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("RefreshToken")
        .WithSummary("Exchange the httpOnly refresh token cookie for a new access token. No request body needed.")
        .AllowAnonymous();

        group.MapPost("/logout", async (ClaimsPrincipal user, ISender sender) =>
        {
            if (!user.TryGetUserId(out var userId)) return Results.Unauthorized();
            var result = await sender.Send(new LogoutCommand(userId));
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .WithName("Logout")
        .WithSummary("Revokes refresh token, deletes Redis session (access token immediately invalid), clears cookie.")
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

        group.MapPost("/forgot-password", async (ForgotPasswordRequest req, ISender sender) =>
        {
            var result = await sender.Send(new ForgotPasswordCommand(req.Email));
            return result.IsSuccess ? Results.Ok(new { message = "If the email exists, a reset code has been sent." }) : result.ToProblemDetails();
        })
        .WithName("ForgotPassword")
        .WithSummary("Request a password reset code via email.")
        .AllowAnonymous();

        group.MapPost("/reset-password", async (ResetPasswordRequest req, ISender sender) =>
        {
            var result = await sender.Send(new ResetPasswordCommand(req.Email, req.Code, req.NewPassword));
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .WithName("ResetPassword")
        .WithSummary("Reset password using the OTP code from forgot-password email.")
        .AllowAnonymous();
    }
}

internal sealed record RegisterRequest(string Email, string Password, string DisplayName);
internal sealed record LoginRequest(string Email, string Password);
internal sealed record SocialLoginRequest(string Provider, string Token);
// RefreshRequest REMOVED — refresh token now comes from httpOnly cookie, not request body
internal sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
internal sealed record ForgotPasswordRequest(string Email);
internal sealed record ResetPasswordRequest(string Email, string Code, string NewPassword);
