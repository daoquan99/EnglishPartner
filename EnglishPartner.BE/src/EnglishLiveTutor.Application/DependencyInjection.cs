using EnglishLiveTutor.Application.Common.Behaviors;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Pronunciation;
using EnglishLiveTutor.Application.Features.Vocabulary;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishLiveTutor.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, string? licenseKey = null)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddValidatorsFromAssembly(assembly);

        var effectiveLicenseKey = licenseKey
            ?? Environment.GetEnvironmentVariable("MEDIATR_LICENSE_KEY")
            ?? Environment.GetEnvironmentVariable("LUCKYPENNY_LICENSE_KEY");

        services.AddMediatR(cfg =>
        {
            if (!string.IsNullOrWhiteSpace(effectiveLicenseKey))
            {
                cfg.LicenseKey = effectiveLicenseKey;
            }

            cfg.RegisterServicesFromAssembly(assembly);

            // MediatR Pipeline Behaviors (Outer -> Inner)
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(AuthorizationBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        // Non-CQRS domain/application services
        services.AddTransient<VocabularyGenerationWriter>();
        services.AddScoped<IPronunciationTargetResolver, PronunciationTargetResolver>();

        // Register custom CQRS handler interfaces (IQueryHandler, ICommandHandler) for direct injection
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract);

        foreach (var type in handlerTypes)
        {
            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType)
                {
                    var genericDef = iface.GetGenericTypeDefinition();
                    if (genericDef == typeof(IQueryHandler<,>) ||
                        genericDef == typeof(ICommandHandler<,>) ||
                        genericDef == typeof(ICommandHandler<>))
                    {
                        services.AddTransient(iface, type);
                    }
                }
            }
        }

        return services;
    }
}
