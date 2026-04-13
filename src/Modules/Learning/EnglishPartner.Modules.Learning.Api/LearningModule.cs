using EnglishPartner.Modules.Learning.Application;
using EnglishPartner.Modules.Learning.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishPartner.Modules.Learning.Api;

public static class LearningModule
{
    public static IServiceCollection AddLearningModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLearningApplication();
        services.AddLearningInfrastructure(configuration);
        return services;
    }

    public static IEndpointRouteBuilder UseLearningModule(this IEndpointRouteBuilder app)
    {
        Endpoints.LearningEndpoints.Map(app);
        return app;
    }
}
