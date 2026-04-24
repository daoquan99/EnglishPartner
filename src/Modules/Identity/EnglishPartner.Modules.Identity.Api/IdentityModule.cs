using EnglishPartner.Modules.Identity.Api.Endpoints;
using EnglishPartner.Modules.Identity.Application;
using EnglishPartner.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishPartner.Modules.Identity.Api;

/// <summary>
/// Identity module installer — called from the API host composition root.
/// </summary>
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityApplication();
        services.AddIdentityInfrastructure(configuration);

        return services;
    }

    public static IEndpointRouteBuilder UseIdentityModule(this IEndpointRouteBuilder app)
    {
        app.MapAuthEndpoints();
        app.MapProfileEndpoints();
        app.MapStreakEndpoints();
        app.MapOnboardingEndpoints();
        app.MapAdminUserEndpoints();
        app.MapAdminRoleEndpoints();

        return app;
    }
}
