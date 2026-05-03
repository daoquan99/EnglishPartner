using EnglishPartner.Messaging;
using EnglishPartner.Modules.Identity.Contracts.IntegrationEvents;
using EnglishPartner.Modules.Learning.Contracts.IntegrationEvents;
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

        // Integration Event Handlers — resolved by InMemoryEventBus
        services.AddScoped<IIntegrationEventHandler<UserRegisteredIntegrationEvent>,
            IntegrationEventHandlers.UserRegisteredIntegrationEventHandler>();
        services.AddScoped<IIntegrationEventHandler<StreakUpdatedIntegrationEvent>,
            IntegrationEventHandlers.StreakUpdatedIntegrationEventHandler>();
        services.AddScoped<IIntegrationEventHandler<LevelUpIntegrationEvent>,
            IntegrationEventHandlers.LevelUpIntegrationEventHandler>();
        services.AddScoped<IIntegrationEventHandler<DailyGoalReminderIntegrationEvent>,
            IntegrationEventHandlers.DailyGoalReminderIntegrationEventHandler>();

        return services;
    }
}
