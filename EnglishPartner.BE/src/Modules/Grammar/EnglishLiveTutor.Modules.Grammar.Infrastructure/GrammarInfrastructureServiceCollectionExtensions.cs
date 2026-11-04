using EnglishLiveTutor.Application.Features.Grammar.ContentPacks;
using EnglishLiveTutor.Application.Features.Grammar.Contracts;
using EnglishLiveTutor.Application.Features.Grammar.Ports;
using EnglishLiveTutor.Application.Modules.Grammar;
using EnglishLiveTutor.Infrastructure.Ai;
using EnglishLiveTutor.Infrastructure.Grammar;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Persistence.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EnglishLiveTutor.Modules.Grammar.Infrastructure;

public static class GrammarInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddGrammarInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddModuleDbContext<GrammarDbContext, IGrammarDbContext>(
            configuration,
            ModuleSchemas.Grammar);

        services.AddScoped<IGrammarProgressReader, GrammarProgressReader>();

        services.AddSingleton<IGrammarContentPackSource, EmbeddedGrammarContentPackSource>();

        services.AddOptions<GrammarGenerationAiOptions>()
            .Bind(configuration.GetSection(GrammarGenerationAiOptions.SectionName))
            .Validate(options => options.TimeoutSeconds > 0 && options.TimeoutSeconds <= 3600,
                "Ai:GrammarGeneration:TimeoutSeconds must be between 1 and 3600.")
            .ValidateOnStart();

        services.AddHttpClient<IGrammarExerciseAiGenerator, GeminiGrammarExerciseAiGenerator>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<GrammarGenerationAiOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        return services;
    }
}
