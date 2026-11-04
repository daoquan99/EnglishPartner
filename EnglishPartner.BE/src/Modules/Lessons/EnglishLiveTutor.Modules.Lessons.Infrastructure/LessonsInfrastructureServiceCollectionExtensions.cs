using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Lessons.Contracts;
using EnglishLiveTutor.Application.Modules.Lessons;
using EnglishLiveTutor.Domain.Lessons.Repositories;
using EnglishLiveTutor.Infrastructure.Ai;
using EnglishLiveTutor.Infrastructure.Lessons;
using EnglishLiveTutor.Infrastructure.Lessons.PromptBuilder;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Persistence.Modules;
using EnglishLiveTutor.Infrastructure.Repositories;
using EnglishLiveTutor.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EnglishLiveTutor.Modules.Lessons.Infrastructure;

public static class LessonsInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddLessonsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddModuleDbContext<LessonsDbContext, ILessonsDbContext>(
            configuration,
            ModuleSchemas.Lessons);

        services.AddScoped<ILessonRolePlayRepository, LessonRolePlayRepository>();
        services.AddScoped<ILessonsProgressReader, LessonsProgressReader>();

        services.AddOptions<LessonMediaStorageOptions>()
            .Bind(configuration.GetSection(LessonMediaStorageOptions.SectionName))
            .Validate(options => options.MaxFileSizeBytes > 0,
                "Storage:LessonMedia:MaxFileSizeBytes must be greater than 0.")
            .ValidateOnStart();
        services.AddSingleton<ILessonMediaStorage, FileLessonMediaStorage>();

        services.AddSingleton<ILessonPromptTemplateProvider, LessonPromptTemplateProvider>();
        services.AddSingleton<ILessonPromptBuilder, LessonPromptBuilder>();

        // Lesson Services
        services.AddSingleton<IDictationScorer, DictationScorer>();
        services.AddSingleton<IDeterministicExerciseGrader, DeterministicExerciseGrader>();
        services.AddSingleton<ILessonSanitizer, LessonSanitizer>();
        services.AddOptions<LessonGenerationOptions>()
            .Bind(configuration.GetSection(LessonGenerationOptions.SectionName))
            .Validate(options => options.TimeoutSeconds is >= 1 and <= 3600,
                "Ai:LessonGeneration:TimeoutSeconds must be between 1 and 3600.")
            .Validate(options => options.MaxRetries is >= 0 and <= 5,
                "Ai:LessonGeneration:MaxRetries must be between 0 and 5.")
            .ValidateOnStart();
        services.AddOptions<LessonAssessmentOptions>()
            .Bind(configuration.GetSection(LessonAssessmentOptions.SectionName))
            .Validate(options => options.TimeoutSeconds is >= 1 and <= 3600,
                "Ai:LessonAssessment:TimeoutSeconds must be between 1 and 3600.")
            .Validate(options => options.MaxRetries is >= 0 and <= 5,
                "Ai:LessonAssessment:MaxRetries must be between 0 and 5.")
            .ValidateOnStart();
        services.AddOptions<LessonRolePlayOptions>()
            .Bind(configuration.GetSection(LessonRolePlayOptions.SectionName))
            .Validate(options => options.TimeoutSeconds is >= 1 and <= 3600,
                "Ai:LessonRolePlay:TimeoutSeconds must be between 1 and 3600.")
            .Validate(options => options.MaxRetries is >= 0 and <= 5,
                "Ai:LessonRolePlay:MaxRetries must be between 0 and 5.")
            .ValidateOnStart();

        services.AddHttpClient<ILessonAiGenerator, GeminiLessonAiGenerator>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<LessonGenerationOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddHttpClient<IRolePlayReplyProvider, GeminiRolePlayReplyProvider>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<LessonRolePlayOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddHttpClient<ILessonAiGrader, LessonAiGrader>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<LessonAssessmentOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddHttpClient<IDictationAiReviewer, DictationAiReviewer>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<LessonAssessmentOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        return services;
    }
}
