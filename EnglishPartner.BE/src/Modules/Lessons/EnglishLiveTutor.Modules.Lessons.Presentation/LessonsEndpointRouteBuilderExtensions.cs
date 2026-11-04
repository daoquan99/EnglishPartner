using Microsoft.AspNetCore.Routing;

namespace EnglishLiveTutor.Api.Endpoints;

public static class LessonsEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapLessonsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapLessonEndpoints();
        app.MapLessonAttemptEndpoints();
        app.MapLessonMediaEndpoints();
        return app;
    }
}
