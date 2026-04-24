using EnglishPartner.Modules.Notifications.Application.Abstractions;
using EnglishPartner.Modules.Notifications.Infrastructure.Persistence;
using EnglishPartner.Modules.Notifications.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishPartner.Modules.Notifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NotificationDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("Database"),
                       npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", NotificationDbContext.Schema));
        });

        services.AddScoped<INotificationUnitOfWork>(sp => sp.GetRequiredService<NotificationDbContext>());
        services.AddScoped<INotificationRepository, NotificationRepository>();

        return services;
    }
}
