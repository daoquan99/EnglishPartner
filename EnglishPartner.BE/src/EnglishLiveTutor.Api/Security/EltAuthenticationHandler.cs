using System.Security.Claims;
using System.Text.Encodings.Web;
using EnglishLiveTutor.Api.Common;
using EnglishLiveTutor.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishLiveTutor.Api.Security;

public sealed class EltAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IJwtTokenService _jwtTokenService;

    public EltAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IJwtTokenService jwtTokenService)
        : base(options, logger, encoder)
    {
        _jwtTokenService = jwtTokenService;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? token = null;
        string? authHeader = Request.Headers.Authorization.FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(authHeader) &&
            authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = authHeader["Bearer ".Length..].Trim();
        }
        else if (Request.Path.StartsWithSegments("/hubs") &&
                 Request.Query.TryGetValue("access_token", out var queryToken))
        {
            token = queryToken.FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var principal = _jwtTokenService.ValidateAccessToken(token);
        if (principal is null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Token không hợp lệ hoặc đã hết hạn."));
        }

        Context.User = principal;
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        await ApiProblem.WriteResponseAsync(
            Response,
            StatusCodes.Status401Unauthorized,
            "Bạn cần đăng nhập để thực hiện thao tác này.");
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        await ApiProblem.WriteResponseAsync(
            Response,
            StatusCodes.Status403Forbidden,
            "Bạn không có quyền truy cập tài nguyên này.");
    }
}
