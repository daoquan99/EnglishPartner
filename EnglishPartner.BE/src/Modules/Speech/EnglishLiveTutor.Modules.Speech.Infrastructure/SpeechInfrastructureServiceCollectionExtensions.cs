using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Features.Pronunciation.Contracts;
using EnglishLiveTutor.Application.Modules.Speech;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;
using EnglishLiveTutor.Infrastructure.Ai;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Persistence.Modules;
using EnglishLiveTutor.Infrastructure.Pronunciation;
using EnglishLiveTutor.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EnglishLiveTutor.Modules.Speech.Infrastructure;

public static class SpeechInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddSpeechInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddModuleDbContext<SpeechDbContext, ISpeechDbContext>(
            configuration,
            ModuleSchemas.Speech);

        // Repositories
        services.AddScoped<IScoringRubricRepository, ScoringRubricRepository>();
        services.AddScoped<IPronunciationAssessmentRepository, PronunciationAssessmentRepository>();
        services.AddScoped<IGeneratedPronunciationAudioRepository, GeneratedPronunciationAudioRepository>();
        services.AddScoped<IAudioRecordingRepository, AudioRecordingRepository>();
        services.AddScoped<ISpeechProgressReader, SpeechProgressReader>();

        // AI Services
        services.AddOptions<PronunciationAssessmentOptions>()
            .Bind(configuration.GetSection(PronunciationAssessmentOptions.SectionName))
            .Validate(options => options.TimeoutSeconds is >= 1 and <= 3600,
                "Ai:PronunciationAssessment:TimeoutSeconds must be between 1 and 3600.")
            .ValidateOnStart();
        services.AddOptions<PronunciationTtsOptions>()
            .Bind(configuration.GetSection(PronunciationTtsOptions.SectionName))
            .Validate(options => options.TimeoutSeconds is >= 1 and <= 3600,
                "Ai:PronunciationTts:TimeoutSeconds must be between 1 and 3600.")
            .ValidateOnStart();
        services.AddHttpClient<IPronunciationAiAssessor, GeminiPronunciationAiAssessor>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<PronunciationAssessmentOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddScoped<ILivePronunciationTtsSynthesizer>(sp => new GeminiLivePronunciationTtsSynthesizer(
            TimeSpan.FromSeconds(sp.GetRequiredService<IOptions<PronunciationTtsOptions>>().Value.TimeoutSeconds)));
        services.AddScoped<EnglishLiveTutor.Modules.Speech.Contracts.ISpeechSynthesisService, EnglishLiveTutor.Modules.Speech.Infrastructure.Ai.SpeechSynthesisService>();
        services.AddSingleton<IPronunciationSynthesisLock, PronunciationSynthesisLock>();

        return services;
    }
}
