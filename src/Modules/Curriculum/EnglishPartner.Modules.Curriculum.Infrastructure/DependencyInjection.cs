using EnglishPartner.Modules.Curriculum.Application.Abstractions;
using EnglishPartner.Modules.Curriculum.Contracts.Services;
using EnglishPartner.Modules.Curriculum.Domain.Conversations.Repositories;
using EnglishPartner.Modules.Curriculum.Domain.ExampleSentences.Repositories;
using EnglishPartner.Modules.Curriculum.Domain.Exercises.Repositories;
using EnglishPartner.Modules.Curriculum.Domain.Grammars.Repositories;
using EnglishPartner.Modules.Curriculum.Domain.Languages.Repositories;
using EnglishPartner.Modules.Curriculum.Domain.Topics.Repositories;
using EnglishPartner.Modules.Curriculum.Domain.Vocabularies.Repositories;
using EnglishPartner.Modules.Curriculum.Infrastructure.Persistence;
using EnglishPartner.Modules.Curriculum.Infrastructure.Persistence.Repositories;
using EnglishPartner.Modules.Curriculum.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishPartner.Modules.Curriculum.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCurriculumInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CurriculumDbContext>((sp, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("Database"),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", CurriculumDbContext.Schema));
        });

        services.AddScoped<ICurriculumUnitOfWork>(sp => sp.GetRequiredService<CurriculumDbContext>());

        services.AddScoped<ILanguageRepository, LanguageRepository>();
        services.AddScoped<ITopicRepository, TopicRepository>();
        services.AddScoped<IVocabularyRepository, VocabularyRepository>();
        services.AddScoped<IGrammarRepository, GrammarRepository>();
        services.AddScoped<IExerciseRepository, ExerciseRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IExampleSentenceRepository, ExampleSentenceRepository>();

        // Module Interface — consumed by other modules (e.g. AI) via Contracts
        services.AddScoped<ICurriculumQueryService, CurriculumQueryService>();

        return services;
    }
}
