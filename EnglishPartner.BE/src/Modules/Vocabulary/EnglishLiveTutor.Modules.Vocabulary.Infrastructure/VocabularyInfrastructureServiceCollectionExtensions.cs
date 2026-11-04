using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Learning.Contracts;
using EnglishLiveTutor.Application.Features.VocabularyGames.Ports;
using EnglishLiveTutor.Application.Modules.Vocabulary;
using EnglishLiveTutor.Domain.Learning.Repositories;
using EnglishLiveTutor.Domain.Vocabulary.Repositories;
using EnglishLiveTutor.Domain.VocabularyGames.Repositories;
using EnglishLiveTutor.Infrastructure.Ai;
using EnglishLiveTutor.Infrastructure.Learning;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Persistence.Modules;
using EnglishLiveTutor.Infrastructure.Repositories;
using EnglishLiveTutor.Infrastructure.VocabularyGames;
using EnglishLiveTutor.Infrastructure.VocabularyGames.Queries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EnglishLiveTutor.Modules.Vocabulary.Infrastructure;

public static class VocabularyInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddVocabularyInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddModuleDbContext<VocabularyDbContext, IVocabularyDbContext>(
            configuration,
            ModuleSchemas.Vocabulary);

        services.AddScoped<IVocabularyRepository, VocabularyRepository>();
        services.AddScoped<IDailyVocabularyPlanRepository, DailyVocabularyPlanRepository>();
        services.AddScoped<IUserVocabularyProgressRepository, UserVocabularyProgressRepository>();
        services.AddScoped<IFallingWordGameRepository, FallingWordGameRepository>();
        services.AddScoped<IFallingWordPoolReader, FallingWordPoolReader>();
        services.AddScoped<IVocabularyProgressReader, VocabularyProgressReader>();

        services.AddOptions<VocabularyGamesOptions>()
            .Bind(configuration.GetSection(VocabularyGamesOptions.SectionName))
            .Validate(options => options.ExpirationPollSeconds >= VocabularyGamesOptions.MinExpirationPollSeconds &&
                                 options.ExpirationPollSeconds <= VocabularyGamesOptions.MaxExpirationPollSeconds,
                $"VocabularyGames:ExpirationPollSeconds must be between {VocabularyGamesOptions.MinExpirationPollSeconds} and {VocabularyGamesOptions.MaxExpirationPollSeconds}.")
            .ValidateOnStart();

        services.AddHostedService<FallingWordRoundExpirationWorker>();

        services.AddSingleton<IVocabularyGenerationContent, VocabularyGenerationContent>();

        services.AddOptions<VocabularyGenerationOptions>()
            .Bind(configuration.GetSection(VocabularyGenerationOptions.SectionName))
            .Validate(options => options.TimeoutSeconds > 0 && options.TimeoutSeconds <= 3600,
                "Ai:VocabularyGeneration:TimeoutSeconds must be between 1 and 3600.")
            .ValidateOnStart();

        services.AddHttpClient<IVocabularyAiGenerator, GeminiVocabularyAiGenerator>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<VocabularyGenerationOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        return services;
    }
}
