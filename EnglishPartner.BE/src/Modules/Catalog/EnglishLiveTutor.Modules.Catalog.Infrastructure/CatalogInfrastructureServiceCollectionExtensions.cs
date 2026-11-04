using EnglishLiveTutor.Application.Modules.Catalog;
using EnglishLiveTutor.Domain.AiCatalog.Repositories;
using EnglishLiveTutor.Domain.Prompts.Repositories;
using EnglishLiveTutor.Domain.Topics.Repositories;
using EnglishLiveTutor.Domain.Voices.Repositories;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Persistence.Modules;
using EnglishLiveTutor.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishLiveTutor.Modules.Catalog.Infrastructure;

public static class CatalogInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddModuleDbContext<CatalogDbContext, ICatalogDbContext>(
            configuration,
            ModuleSchemas.Catalog);

        services.AddScoped<IPromptConfigurationRepository, PromptConfigurationRepository>();
        services.AddScoped<ISpeechSpeedRepository, SpeechSpeedRepository>();
        services.AddScoped<ITeacherVoiceRepository, TeacherVoiceRepository>();
        services.AddScoped<ITopicRepository, TopicRepository>();
        services.AddScoped<IAiModelCatalogRepository, AiModelCatalogRepository>();

        return services;
    }
}
