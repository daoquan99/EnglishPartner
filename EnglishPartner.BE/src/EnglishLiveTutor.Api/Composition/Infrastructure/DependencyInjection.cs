using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Modules;
using EnglishLiveTutor.Application.Modules.Catalog;
using EnglishLiveTutor.Application.Modules.Grammar;
using EnglishLiveTutor.Application.Modules.Identity;
using EnglishLiveTutor.Application.Modules.Lessons;
using EnglishLiveTutor.Application.Modules.LiveTutor;
using EnglishLiveTutor.Application.Modules.Speech;
using EnglishLiveTutor.Application.Modules.Vocabulary;
using EnglishLiveTutor.Application.Common.Persistence;
using EnglishLiveTutor.Domain.AiCatalog.Repositories;
using EnglishLiveTutor.Domain.Conversations.Repositories;
using EnglishLiveTutor.Domain.Identity.Repositories;
using EnglishLiveTutor.Domain.Prompts.Repositories;
using EnglishLiveTutor.Domain.Pronunciation.Repositories;
using EnglishLiveTutor.Domain.Topics.Repositories;
using EnglishLiveTutor.Domain.Voices.Repositories;
using EnglishLiveTutor.Application.Features.Conversations.Ports;
using EnglishLiveTutor.Infrastructure.Ai;
using EnglishLiveTutor.Infrastructure.Identity;
using EnglishLiveTutor.Infrastructure.Persistence;
using EnglishLiveTutor.Infrastructure.Persistence.Interceptors;
using EnglishLiveTutor.Infrastructure.Persistence.Modules;
using EnglishLiveTutor.Infrastructure.Realtime;
using EnglishLiveTutor.Infrastructure.Repositories;
using EnglishLiveTutor.Modules.Catalog.Infrastructure;
using EnglishLiveTutor.Modules.Identity.Infrastructure;
using EnglishLiveTutor.Modules.Speech.Infrastructure;
using EnglishLiveTutor.Modules.Vocabulary.Infrastructure;
using EnglishLiveTutor.Modules.Lessons.Infrastructure;
using EnglishLiveTutor.Modules.Grammar.Infrastructure;
using EnglishLiveTutor.Modules.LiveTutor.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishLiveTutor.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("Connection string is not configured. Please configure ConnectionStrings:DefaultConnection or ConnectionStrings:SqlServer.");

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<AuditingSaveChangesInterceptor>();
        services.AddScoped(_ => new SqlConnection(connectionString));

        services.AddSharedDbContext(configuration);

        services.AddIdentityInfrastructure(configuration);
        services.AddCatalogInfrastructure(configuration);
        services.AddSpeechInfrastructure(configuration);
        services.AddLiveTutorInfrastructure(configuration);
        services.AddLessonsInfrastructure(configuration);
        services.AddGrammarInfrastructure(configuration);
        services.AddVocabularyInfrastructure(configuration);

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddHostedService<DbContextWarmupService>();
        services.AddSingleton(typeof(IDatabaseContextMigrator<>), typeof(DatabaseContextMigrator<>));
        services.AddSingleton<IDatabaseMigrationRunner, DatabaseMigrationRunner>();

        // Aggregate Repositories
        services.AddScoped<IIdempotencyService, IdempotencyService>();

        // AI & Realtime Services
        services.AddOptions<AntigravityCliOptions>()
            .Bind(configuration.GetSection(AntigravityCliOptions.SectionName))
            .Validate(options => options.TimeoutSeconds is >= 1 and <= 3600,
                "Ai:AntigravityCli:TimeoutSeconds must be between 1 and 3600.")
            .Validate(options => options.AllowedModels != null && options.AllowedModels.Count > 0,
                "Ai:AntigravityCli:AllowedModels must not be empty.")
            .ValidateOnStart();
        services.AddSingleton<IAgyCliRunner, AgyCliRunner>();

        return services;
    }
}
