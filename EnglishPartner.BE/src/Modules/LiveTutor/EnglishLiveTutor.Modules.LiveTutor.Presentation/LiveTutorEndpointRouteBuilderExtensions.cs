using Microsoft.AspNetCore.Routing;

namespace EnglishLiveTutor.Api.Endpoints;

public static class LiveTutorEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapLiveTutorEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapConversationEndpoints();
        return app;
    }
}
