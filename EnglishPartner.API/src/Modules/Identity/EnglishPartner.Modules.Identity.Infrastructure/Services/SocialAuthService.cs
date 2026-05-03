using EnglishPartner.Modules.Identity.Application.Abstractions;
using EnglishPartner.Modules.Identity.Application.Auth.Settings;
using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace EnglishPartner.Modules.Identity.Infrastructure.Services;

internal sealed class SocialAuthService(
    IOptions<SocialAuthSettings> settings,
    ILogger<SocialAuthService> logger) : ISocialAuthService
{
    private readonly SocialAuthSettings _settings = settings.Value;
    
    // Cache the configuration manager to avoid fetching keys on every request (it handles caching internally)
    private static ConfigurationManager<OpenIdConnectConfiguration>? _msalConfigManager;


    public async Task<SocialUserInfo?> VerifyTokenAsync(string provider, string token, CancellationToken cancellationToken = default)
    {
        return provider.ToLowerInvariant() switch
        {
            "google" => await VerifyGoogleTokenAsync(token),
            "microsoft" => await VerifyMicrosoftTokenAsync(token, cancellationToken),
            _ => throw new NotSupportedException($"Social authentication provider '{provider}' is not supported yet.")
        };
    }

    private async Task<SocialUserInfo?> VerifyGoogleTokenAsync(string idToken)
    {
        try
        {
            var validationSettings = new GoogleJsonWebSignature.ValidationSettings();
            
            // If ClientId is configured, validate audience.
            if (!string.IsNullOrWhiteSpace(_settings.Google.ClientId))
            {
                validationSettings.Audience = new[] { _settings.Google.ClientId };
            }

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);

            if (payload is null)
                return null;

            return new SocialUserInfo(
                "Google",
                payload.Subject, // the unique Google ID
                payload.Email,
                payload.GivenName,
                payload.FamilyName,
                payload.Picture);
        }
        catch (InvalidJwtException ex)
        {
            logger.LogWarning(ex, "Invalid Google ID token provided.");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while verifying Google ID token.");
            return null;
        }
    }

    private async Task<SocialUserInfo?> VerifyMicrosoftTokenAsync(string token, CancellationToken cancellationToken)
    {
        try
        {
            var tenantId = string.IsNullOrWhiteSpace(_settings.Microsoft.TenantId) ? "common" : _settings.Microsoft.TenantId;
            var discoveryEndpoint = $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration";

            _msalConfigManager ??= new ConfigurationManager<OpenIdConnectConfiguration>(
                discoveryEndpoint,
                new OpenIdConnectConfigurationRetriever());

            var openIdConfig = await _msalConfigManager.GetConfigurationAsync(cancellationToken);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false, // common tenant applies to multiple issuers
                ValidateAudience = !string.IsNullOrWhiteSpace(_settings.Microsoft.ClientId),
                ValidAudience = _settings.Microsoft.ClientId,
                ValidateLifetime = true,
                IssuerSigningKeys = openIdConfig.SigningKeys
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            var oid = principal.FindFirst("oid")?.Value ?? principal.FindFirst("sub")?.Value;
            var email = principal.FindFirst("preferred_username")?.Value ?? principal.FindFirst("email")?.Value;
            var name = principal.FindFirst("name")?.Value;

            if (oid is null || email is null)
            {
                logger.LogWarning("Microsoft token missing required claims (oid, preferred_username).");
                return null;
            }

            // Attempt to split name into first/last
            string? firstName = name;
            string? lastName = null;
            if (name != null)
            {
                var parts = name.Split(' ', 2);
                firstName = parts[0];
                if (parts.Length > 1) lastName = parts[1];
            }

            return new SocialUserInfo(
                "Microsoft",
                oid,
                email,
                firstName,
                lastName,
                null // avatar usually not in MS JWT directly
            );
        }
        catch (SecurityTokenException ex)
        {
            logger.LogWarning(ex, "Invalid Microsoft token provided.");
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while verifying Microsoft token.");
            return null;
        }
    }
}
