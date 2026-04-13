using EnglishPartner.Modules.Notifications.Application;
using EnglishPartner.Modules.Notifications.Infrastructure;
using EnglishPartner.Modules.Notifications.Api.Endpoints;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace EnglishPartner.Modules.Notifications.Api;

public static class NotificationModule
{
    public static IServiceCollection AddNotificationModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddNotificationApplication(configuration);
        services.AddNotificationInfrastructure(configuration);

        return services;
    }

    public static IEndpointRouteBuilder UseNotificationModule(this IEndpointRouteBuilder app)
    {
        app.MapNotificationEndpoints();
        return app;
    }
}
