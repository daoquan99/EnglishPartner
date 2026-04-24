using EnglishPartner.Modules.Curriculum.Application.Vocabularies.Commands.CreateVocabulary;
using EnglishPartner.Modules.Curriculum.Application.Vocabularies.Commands.DeleteVocabulary;
using EnglishPartner.Modules.Curriculum.Application.Vocabularies.Commands.UpdateVocabulary;
using EnglishPartner.Modules.Curriculum.Application.Vocabularies.Queries.GetVocabularies;
using EnglishPartner.Modules.Curriculum.Application.Vocabularies.Queries.GetVocabularyById;
using EnglishPartner.Modules.Curriculum.Application.Vocabularies.Queries.LookupVocabularies;
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
            .RequireAuthorization("curriculum.vocabulary.write"); // permission implies Admin role

        // ── User Endpoints ──
        group.MapGet("", async (
            string languageCode, int page, int pageSize,
            string? searchTerm, string? level, Guid? topicId,
            ISender sender) =>
        {
            var result = await sender.Send(new GetVocabulariesQuery(languageCode, page, pageSize, searchTerm, level, topicId));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetVocabularies")
        .WithSummary("Get a paginated list of vocabulary items. Supports filtering by searchTerm, level, topicId.");

        group.MapGet("{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetVocabularyByIdQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetVocabularyById")
        .WithSummary("Get a single vocabulary item by ID, including its example sentences.");

        adminGroup.MapGet("lookup", async (
            string languageCode, string? searchTerm, int page, int pageSize, ISender sender) =>
        {
            var result = await sender.Send(new LookupVocabulariesQuery(languageCode, searchTerm, page, pageSize));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("LookupVocabularies")
        .WithSummary("Get a paginated lookup list of vocabularies (id, label) for admin UI.");

        // ── Admin Endpoints ──
        adminGroup.MapPost("", async (CreateVocabularyCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/v1/curriculum/vocabularies/{result.Value}", result.Value)
                : result.ToProblemDetails();
        })
        .WithName("CreateVocabulary")
        .WithSummary("Create a new vocabulary item.");

        adminGroup.MapPut("{id:guid}", async (Guid id, UpdateVocabularyCommand command, ISender sender) =>
        {
            if (id != command.Id) return Results.BadRequest("Route ID does not match body ID.");
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .WithName("UpdateVocabulary")
        .WithSummary("Update an existing vocabulary item, including synonyms and linked topic.");

        adminGroup.MapDelete("{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteVocabularyCommand(id));
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .WithName("DeleteVocabulary")
        .WithSummary("Soft-delete a vocabulary item.");
    }
}
