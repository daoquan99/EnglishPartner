using EnglishLiveTutor.Application.Features.AiModels.Contracts;
using EnglishLiveTutor.Application.Features.AiModels.Services;
using EnglishLiveTutor.Application.Features.Prompts.Contracts;
using EnglishLiveTutor.Application.Features.Prompts.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishLiveTutor.Modules.Catalog;

public static class CatalogModuleServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services)
    {
        services.AddScoped<IAiModelCatalogReader, AiModelCatalogReader>();
        services.AddSingleton<IPromptCatalogReader, PromptCatalogReader>();

        return services;
    }
}
