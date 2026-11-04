using EnglishLiveTutor.Application.Common.Behaviors;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Modules.Catalog;
using EnglishLiveTutor.Modules.Grammar;
using EnglishLiveTutor.Modules.Identity;
using EnglishLiveTutor.Modules.Lessons;
using EnglishLiveTutor.Modules.LiveTutor;
using EnglishLiveTutor.Modules.Progress;
using EnglishLiveTutor.Modules.Speech;
using EnglishLiveTutor.Modules.Vocabulary;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishLiveTutor.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, string? licenseKey = null)
    {
        services.AddValidatorsFromAssemblies(ApplicationAssemblies.All);

        var effectiveLicenseKey = licenseKey
            ?? Environment.GetEnvironmentVariable("MEDIATR_LICENSE_KEY")
            ?? Environment.GetEnvironmentVariable("LUCKYPENNY_LICENSE_KEY");

        services.AddMediatR(cfg =>
        {
            if (!string.IsNullOrWhiteSpace(effectiveLicenseKey))
            {
                cfg.LicenseKey = effectiveLicenseKey;
            }

            cfg.RegisterServicesFromAssemblies(ApplicationAssemblies.All.ToArray());

            // MediatR Pipeline Behaviors (Outer -> Inner)
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(AuthorizationBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
            cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        // Non-CQRS domain/application services
        services.AddCatalogModule();
        services.AddIdentityModule();
        services.AddSpeechModule();
        services.AddVocabularyModule();
        services.AddLessonsModule();
        services.AddGrammarModule();
        services.AddLiveTutorModule();
        services.AddProgressModule();

        return services;
    }
}
