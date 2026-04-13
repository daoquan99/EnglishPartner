using EnglishPartner.Modules.Curriculum.Application.Vocabularies.Commands.CreateVocabulary;
using EnglishPartner.Modules.Curriculum.Application.Vocabularies.Queries.GetVocabularies;
using EnglishPartner.Modules.Curriculum.Application.Vocabularies.Queries.GetVocabularyById;
using EnglishPartner.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnglishPartner.Modules.Curriculum.Api.Endpoints;

internal static class VocabularyEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/curriculum/vocabularies")
            .WithTags("Vocabularies")
            .RequireAuthorization();

        var adminGroup = app.MapGroup("api/v1/admin/curriculum/vocabularies")
            .WithTags("Admin: Vocabularies")
            .RequireAuthorization("Admin");

        // ── User Endpoints ──
        group.MapGet("", async (string languageCode, int page, int pageSize, ISender sender) =>
        {
            var result = await sender.Send(new GetVocabulariesQuery(languageCode, page, pageSize));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        });

        group.MapGet("{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetVocabularyByIdQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        });

        // ── Admin Endpoints ──
        adminGroup.MapPost("", async (CreateVocabularyCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Created($"/api/v1/curriculum/vocabularies/{result.Value}", result.Value) : result.ToProblemDetails();
        });
    }
}
