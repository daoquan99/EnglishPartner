using EnglishPartner.Modules.Curriculum.Application.Behaviors;
using EnglishPartner.SharedKernel.Application.Caching;
using EnglishPartner.SharedKernel.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishPartner.Modules.Curriculum.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCurriculumApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);
            config.LicenseKey = Environment.GetEnvironmentVariable("MEDIATR_LICENSE") ?? string.Empty;
            config.AddOpenBehavior(typeof(LoggingBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
            config.AddOpenBehavior(typeof(QueryCachingBehavior<,>));
            config.AddOpenBehavior(typeof(CurriculumUnitOfWorkBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}

