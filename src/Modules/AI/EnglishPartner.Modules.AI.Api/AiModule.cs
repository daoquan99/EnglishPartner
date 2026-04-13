using EnglishPartner.Modules.AI.Application;
using EnglishPartner.Modules.AI.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishPartner.Modules.AI.Api;

/// <summary>
/// AI Module installer — wires Application + Infrastructure + Endpoints.
/// </summary>
public static class AiModule
{
    public static IServiceCollection AddAiModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAiApplication();
        services.AddAiInfrastructure(configuration);
        return services;
    }

    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        Endpoints.AiEndpoints.Map(app);
        return app;
    }
}
