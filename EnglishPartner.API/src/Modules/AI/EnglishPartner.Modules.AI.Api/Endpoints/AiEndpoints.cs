using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnglishPartner.Modules.AI.Api.Endpoints;

internal static class AiEndpoints
{
    internal static void Map(IEndpointRouteBuilder app)
    {
        // ── Authenticated user group ──
        var group = app.MapGroup("api/v1/ai")
            .WithTags("AI")
            .RequireAuthorization(); // all AI endpoints require login

        EvaluateEndpoints.Map(group);
        TranslateEndpoints.Map(group);
        SpeechEndpoints.Map(group);
        ChatEndpoints.Map(group);

        // ── Admin group ──
        var admin = app.MapGroup("api/v1/admin/ai")
            .WithTags("AI Admin")
            .RequireAuthorization("ai.logs.read");

        AdminEndpoints.Map(admin);
    }
}
