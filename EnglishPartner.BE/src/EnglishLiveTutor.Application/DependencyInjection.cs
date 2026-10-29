using EnglishLiveTutor.Application.Common.Behaviors;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Grammar.ContentPacks;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Application.Features.Grammar.Services;
using EnglishLiveTutor.Application.Features.Learning.Services;
using EnglishLiveTutor.Application.Features.Lessons.Services;
using EnglishLiveTutor.Application.Features.Lessons.Validation;
using EnglishLiveTutor.Application.Features.Pronunciation;
using EnglishLiveTutor.Application.Features.Vocabulary;
using EnglishLiveTutor.Application.Features.Vocabulary.Services;
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
        services.AddScoped<VocabularyGenerationWriter>();
        services.AddScoped<IPronunciationTargetResolver, PronunciationTargetResolver>();
        services.AddScoped<ILessonCandidateValidator, LessonCandidateValidator>();

        // Application services extracted for shared logic (no handler injects another handler)
        services.AddScoped<ILessonAttemptResumeBuilder, LessonAttemptResumeBuilder>();
        services.AddScoped<ILessonDetailBuilder, LessonDetailBuilder>();
        services.AddScoped<ILessonRubricDetailBuilder, LessonRubricDetailBuilder>();
        services.AddScoped<IVocabularyPromptBuilder, VocabularyPromptBuilder>();
        services.AddScoped<IDailyVocabularyPlanInitializer, DailyVocabularyPlanInitializer>();

        // Grammar Content Pack Importer & Sanitizer
        services.AddScoped<GrammarContentPackImporter>();
        services.AddSingleton<IGrammarLearnerContentSanitizer, GrammarLearnerContentSanitizer>();

        // Grammar Exercise Generation services
        services.AddSingleton<IGrammarExercisePromptBuilder, GrammarExercisePromptBuilder>();
        services.AddScoped<IGrammarExercisePromptContextLoader, GrammarExercisePromptContextLoader>();
        services.AddSingleton<IGrammarExerciseOutputProcessor, GrammarExerciseOutputProcessor>();

        return services;
    }
}
