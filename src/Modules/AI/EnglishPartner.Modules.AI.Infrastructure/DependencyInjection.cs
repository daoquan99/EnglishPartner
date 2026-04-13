using EnglishPartner.Modules.AI.Application.Abstractions;
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
            options.UseNpgsql(connectionString)
                .AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        services.AddScoped<IAiUnitOfWork>(sp => sp.GetRequiredService<AiDbContext>());
        services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
        services.AddScoped<IAiRequestLogRepository, AiRequestLogRepository>();

        // Gemini HTTP Client — API key injected as header (not URL query param)
        services.AddHttpClient<IGeminiService, GeminiService>((sp, client) =>
        {
            var config = sp.GetRequiredService<IOptions<GeminiAISettings>>().Value;
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("x-goog-api-key", config.ApiKey);
        })
        .AddStandardResilienceHandler();

        return services;
    }
}
