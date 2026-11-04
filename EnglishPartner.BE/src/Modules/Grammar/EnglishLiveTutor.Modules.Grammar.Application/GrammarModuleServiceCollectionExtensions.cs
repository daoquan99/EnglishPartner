using EnglishLiveTutor.Application.Features.Grammar.ContentPacks;
using EnglishLiveTutor.Application.Features.Grammar.Models;
using EnglishLiveTutor.Application.Features.Grammar.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishLiveTutor.Modules.Grammar;

public static class GrammarModuleServiceCollectionExtensions
{
    public static IServiceCollection AddGrammarModule(this IServiceCollection services)
    {
        services.AddScoped<GrammarContentPackImporter>();
        services.AddSingleton<IGrammarLearnerContentSanitizer, GrammarLearnerContentSanitizer>();
        services.AddSingleton<IGrammarExercisePromptBuilder, GrammarExercisePromptBuilder>();
        services.AddScoped<IGrammarExercisePromptContextLoader, GrammarExercisePromptContextLoader>();
        services.AddSingleton<IGrammarExerciseOutputProcessor, GrammarExerciseOutputProcessor>();

        return services;
    }
}
