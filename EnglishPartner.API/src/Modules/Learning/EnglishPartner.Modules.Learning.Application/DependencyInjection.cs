using EnglishPartner.Modules.Learning.Application.Behaviors;
using EnglishPartner.SharedKernel.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishPartner.Modules.Learning.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddLearningApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);
            config.LicenseKey = Environment.GetEnvironmentVariable("MEDIATR_LICENSE") ?? string.Empty;
            config.AddOpenBehavior(typeof(LoggingBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
            config.AddOpenBehavior(typeof(LearningUnitOfWorkBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}

