using Microsoft.AspNetCore.Routing;

namespace EnglishLiveTutor.Api.Endpoints;

public static class VocabularyEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapVocabularyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapVocabularyCoreEndpoints();
        app.MapDailyLearningEndpoints();
        app.MapVocabularyGamesEndpoints();
        return app;
    }
}
