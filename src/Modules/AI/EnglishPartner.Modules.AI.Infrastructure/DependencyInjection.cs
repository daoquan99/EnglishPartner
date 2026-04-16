using EnglishPartner.Modules.AI.Application.Abstractions;
using EnglishPartner.Modules.AI.Contracts.Services;
using EnglishPartner.Modules.AI.Domain.AiRequestLogs.Repositories;
using EnglishPartner.Modules.AI.Domain.ChatSessions.Repositories;
using EnglishPartner.Modules.AI.Infrastructure.Persistence;
using EnglishPartner.Modules.AI.Infrastructure.Persistence.Repositories;
using EnglishPartner.Modules.AI.Infrastructure.Services;
using EnglishPartner.SharedKernel.Interceptors;
using EnglishPartner.SharedKernel.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EnglishPartner.Modules.AI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAiInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        services.AddDbContext<AiDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "ai"));
        });

        services.AddScoped<IAiUnitOfWork>(sp => sp.GetRequiredService<AiDbContext>());
        services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
        services.AddScoped<IAiRequestLogRepository, AiRequestLogRepository>();
        services.AddScoped<IAiEvaluationService, AiEvaluationService>();
        services.AddScoped<ICurriculumContentGenerationService, CurriculumContentGenerationService>();

        // Vertex AI Express Mode — HTTP Client
        // Auth: x-goog-api-key header (API key from GCP Console)
        // Endpoint: https://aiplatform.googleapis.com/v1/publishers/google/models/{model}:generateContent
        //
        // Why no resilience handler?
        //   - Retrying AI timeouts wastes quota and doubles latency
        //   - Circuit breaker adds no value for a single external AI provider
        //   → HttpClient.Timeout is the correct and sufficient control here
        var geminiTimeoutSeconds = configuration.GetValue<int>("GeminiAI:TimeoutSeconds", defaultValue: 120);

        //services.AddHttpClient<IGeminiService, GeminiService>((sp, client) =>
        //{
        //    var config = sp.GetRequiredService<IOptions<GeminiAISettings>>().Value;
        //    client.Timeout = TimeSpan.FromSeconds(geminiTimeoutSeconds);
        //    client.DefaultRequestHeaders.Add("x-goog-api-key", config.ApiKey);
        //});

        //services.AddScoped<IGeminiService, GoogleCloudGeminiService>();
        //services.AddScoped<IGeminiService, GoogleGenAIGeminiService>();
        services.AddScoped<IGeminiService, MsccGeminiService>();

        // Google Cloud TTS + STT via official SDK (Google.Cloud.TextToSpeech.V1 / Google.Cloud.Speech.V1)
        // Auth: ADC — SDK picks up credentials automatically (gcloud auth application-default login)
        // No HttpClient needed — SDK uses gRPC transport internally
        services.AddScoped<ISpeechService, SpeechService>();

        // Native Audio Live API
        services.AddTransient<ILiveAudioSession, GoogleGenAILiveAudioSession>();
        services.AddSingleton<LiveAudioSessionManager>();

        // Gemini native TTS via HTTP REST (v1beta1)
        services.AddHttpClient("GeminiTTS");
        services.AddScoped<IGeminiTtsService, GeminiTtsService>();

        return services;
    }
}


