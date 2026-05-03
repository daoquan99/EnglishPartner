using EnglishPartner.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnglishPartner.Modules.AI.Api.Endpoints;

internal static class TranslateEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/translate", async ([Microsoft.AspNetCore.Mvc.FromBody] TranslateRequest request, ISender sender) =>
        {
            var query = new EnglishPartner.Modules.AI.Application.Translation.Queries.Translate.TranslateQuery(request.Text, request.TargetLanguageCode);
            var result = await sender.Send(query);
            return result.IsSuccess ? Results.Ok(new { translation = result.Value }) : result.ToProblemDetails();
        })
        .WithName("Translate")
        .WithSummary("Translate text to a target language using AI.")
        .RequireRateLimiting("ai-scoring");
    }
}

public sealed record TranslateRequest(string Text, string TargetLanguageCode);
