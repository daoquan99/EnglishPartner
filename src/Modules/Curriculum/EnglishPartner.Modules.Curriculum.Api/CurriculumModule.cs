using EnglishPartner.Modules.Curriculum.Application;
using EnglishPartner.Modules.Curriculum.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishPartner.Modules.Curriculum.Api;

public static class CurriculumModule
{
    public static IServiceCollection AddCurriculumModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCurriculumApplication();
        services.AddCurriculumInfrastructure(configuration);
        return services;
    }

    public static IEndpointRouteBuilder UseCurriculumModule(this IEndpointRouteBuilder app)
    {
        Endpoints.TopicEndpoints.Map(app);
        Endpoints.VocabularyEndpoints.Map(app);
        Endpoints.GrammarEndpoints.Map(app);
        Endpoints.ExerciseEndpoints.Map(app);
        Endpoints.ConversationEndpoints.Map(app);
        Endpoints.ExampleSentenceEndpoints.Map(app);
        Endpoints.CurriculumGenerateEndpoints.Map(app);
        return app;
    }
}
