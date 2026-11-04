using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Conversations.Contracts;
using EnglishLiveTutor.Application.Features.Conversations.Ports;
using EnglishLiveTutor.Application.Modules.LiveTutor;
using EnglishLiveTutor.Domain.Conversations.Repositories;
using EnglishLiveTutor.Infrastructure.Ai;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Persistence.Modules;
using EnglishLiveTutor.Infrastructure.Realtime;
using EnglishLiveTutor.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishLiveTutor.Modules.LiveTutor.Infrastructure;

public static class LiveTutorInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddLiveTutorInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.TryAddSingleton<ILiveSessionRealtimeNotifier>(NullLiveSessionRealtimeNotifier.Instance);

        services.AddModuleDbContext<LiveTutorDbContext, ILiveTutorDbContext>(
            configuration,
            ModuleSchemas.LiveTutor);

        services.AddScoped<ILiveTutorProgressReader, LiveTutorProgressReader>();

        // Aggregate Repositories
        services.AddSingleton<IConversationRepository, ConversationRepository>();

        // AI & Realtime Services
        services.AddSingleton<ICefrPromptBuilder, CefrPromptBuilder>();
        services.AddHttpClient();
        services.AddOptions<LiveTutorAiOptions>()
            .Bind(configuration.GetSection(LiveTutorAiOptions.SectionName))
            .Validate(o => o.TranslationFallbackTimeoutSeconds is >= 1 and <= 3600 && o.GrammarCorrectionTimeoutSeconds is >= 1 and <= 3600,
                "Ai:LiveTutor timeouts must be between 1 and 3600 seconds.")
            .ValidateOnStart();
        services.AddSingleton(sp => new FastTextTranslatorFallback(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
            TimeSpan.FromSeconds(sp.GetRequiredService<IOptions<LiveTutorAiOptions>>().Value.TranslationFallbackTimeoutSeconds)));
        services.AddSingleton(sp => new GrammarCorrectionAnalyzerService(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
            sp.GetRequiredService<FastTextTranslatorFallback>(),
            TimeSpan.FromSeconds(sp.GetRequiredService<IOptions<LiveTutorAiOptions>>().Value.GrammarCorrectionTimeoutSeconds),
            sp.GetRequiredService<IOptions<LiveTutorAiOptions>>(),
            sp.GetService<ILogger<GrammarCorrectionAnalyzerService>>()));

        services.AddOptions<LiveSessionOptions>()
            .Bind(configuration.GetSection(LiveSessionOptions.SectionName));
        services.AddSingleton<GeminiLiveConnectionManager>();
        services.AddSingleton<ILiveSessionService>(sp => sp.GetRequiredService<GeminiLiveConnectionManager>());

        return services;
    }
}
