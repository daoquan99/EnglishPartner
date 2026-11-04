using Microsoft.AspNetCore.Routing;

namespace EnglishLiveTutor.Api.Endpoints;

public static class GrammarEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapGrammarEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGrammarCoreEndpoints();
        app.MapAdminGrammarEndpoints();
        return app;
    }
}
