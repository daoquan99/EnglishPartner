using Microsoft.AspNetCore.Routing;

namespace EnglishLiveTutor.Api.Endpoints;

public static class CatalogEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPromptEndpoints();
        app.MapVoiceEndpoints();
        app.MapTopicEndpoints();
        app.MapAiModelEndpoints();
        return app;
    }
}
