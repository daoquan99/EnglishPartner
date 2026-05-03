using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace EnglishPartner.SharedKernel.Application.Auth;

/// <summary>
/// Dynamically provides authorization policies based on permission names.
/// Instead of registering 1000s of policies in Startup, this provider creates them on the fly.
/// </summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) 
    : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // First check if the policy was explicitly registered in AuthorizationOptions
        var policy = await base.GetPolicyAsync(policyName);
        if (policy is not null)
        {
            return policy;
        }

        // If not registered but it looks like a permission requirement (or is the fallback "Admin" policy),
        // we generate a policy on the fly that checks for the "permission" claim.
        // We can treat "Admin" policy as checking for the "admin" permission.
        var permissionClaim = policyName.Equals("Admin", StringComparison.OrdinalIgnoreCase) 
            ? "admin" 
            : policyName;

        return new AuthorizationPolicyBuilder()
            .RequireClaim("permission", permissionClaim)
            .Build();
    }
}
