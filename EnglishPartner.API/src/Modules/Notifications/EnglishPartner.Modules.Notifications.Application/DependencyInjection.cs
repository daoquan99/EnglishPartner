using System.Reflection;
using EnglishPartner.Modules.Notifications.Application.Abstractions;
using EnglishPartner.Modules.Notifications.Application.Behaviors;
using EnglishPartner.Modules.Notifications.Application.Settings;
using EnglishPartner.SharedKernel.CQRS;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishPartner.Modules.Notifications.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<NotificationSettings>(configuration.GetSection(NotificationSettings.SectionName));

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.LicenseKey = Environment.GetEnvironmentVariable("MEDIATR_LICENSE") ?? string.Empty;

            // Unit of Work behavior — saves changes after every Notification command
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(NotificationUnitOfWorkBehavior<,>));
        });

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), includeInternalTypes: true);

        return services;
    }
}
