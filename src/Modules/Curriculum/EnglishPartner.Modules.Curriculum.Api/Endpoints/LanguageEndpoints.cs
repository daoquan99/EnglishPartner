using EnglishPartner.Modules.Curriculum.Application.Languages.Commands.CreateLanguage;
using EnglishPartner.Modules.Curriculum.Application.Languages.Commands.DeleteLanguage;
using EnglishPartner.Modules.Curriculum.Application.Languages.Commands.UpdateLanguage;
using EnglishPartner.Modules.Curriculum.Application.Languages.Queries.GetLanguages;
using EnglishPartner.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnglishPartner.Modules.Curriculum.Api.Endpoints;

internal static class LanguageEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/curriculum/languages").WithTags("Curriculum Languages");

        group.MapGet("/", async (ISender sender) =>
        {
            var result = await sender.Send(new GetLanguagesQuery());
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetLanguages")
        .WithSummary("Get a list of all supported languages")
        .RequireAuthorization();

        group.MapPost("/", async (CreateLanguageRequest req, ISender sender) =>
        {
            var result = await sender.Send(new CreateLanguageCommand(req.Code, req.Name, req.NativeName, req.SortOrder));
            return result.IsSuccess ? Results.Created($"/api/v1/curriculum/languages/{result.Value}", new { id = result.Value }) : result.ToProblemDetails();
        })
        .WithName("CreateLanguage")
        .WithSummary("Create a new supported language")
        .RequireAuthorization(policy => policy.RequireClaim("permissions", "curriculum.topics.write")); // Reusing an existing curriculum admin permission

        group.MapPut("/{id:guid}", async (Guid id, UpdateLanguageRequest req, ISender sender) =>
        {
            var result = await sender.Send(new UpdateLanguageCommand(id, req.Name, req.NativeName, req.SortOrder));
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .WithName("UpdateLanguage")
        .WithSummary("Update a language")
        .RequireAuthorization(policy => policy.RequireClaim("permissions", "curriculum.topics.write"));

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteLanguageCommand(id));
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .WithName("DeleteLanguage")
        .WithSummary("Delete a language")
        .RequireAuthorization(policy => policy.RequireClaim("permissions", "curriculum.topics.write"));
    }
}

internal sealed record CreateLanguageRequest(string Code, string Name, string NativeName, int SortOrder);
internal sealed record UpdateLanguageRequest(string Name, string NativeName, int SortOrder);

