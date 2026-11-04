using Microsoft.AspNetCore.Routing;

namespace EnglishLiveTutor.Api.Endpoints;

public static class SpeechEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapSpeechEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPronunciationEndpoints();
        app.MapLessonRubricEndpoints();
        return app;
    }
}
