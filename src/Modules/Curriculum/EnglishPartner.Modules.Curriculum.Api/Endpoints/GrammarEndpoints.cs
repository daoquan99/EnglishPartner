using EnglishPartner.Modules.Curriculum.Application.Grammars.Commands.CreateGrammar;
using EnglishPartner.Modules.Curriculum.Application.Grammars.Queries.GetGrammarById;
using EnglishPartner.Modules.Curriculum.Application.Grammars.Queries.GetGrammars;
using EnglishPartner.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnglishPartner.Modules.Curriculum.Api.Endpoints;

internal static class GrammarEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/curriculum/grammars")
            .WithTags("Grammars")
            .RequireAuthorization();

        var adminGroup = app.MapGroup("api/v1/admin/curriculum/grammars")
            .WithTags("Admin: Grammars")
            .RequireAuthorization("Admin");

        // ── User Endpoints ──
        group.MapGet("", async (string languageCode, ISender sender) =>
        {
            var result = await sender.Send(new GetGrammarsQuery(languageCode));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        });

        group.MapGet("{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetGrammarByIdQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        });

        // ── Admin Endpoints ──
        adminGroup.MapPost("", async (CreateGrammarCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Created($"/api/v1/curriculum/grammars/{result.Value}", result.Value) : result.ToProblemDetails();
        });

        adminGroup.MapPut("{id:guid}", async (Guid id, EnglishPartner.Modules.Curriculum.Application.Grammars.Commands.UpdateGrammar.UpdateGrammarCommand command, ISender sender) =>
        {
            if (id != command.Id) return Results.BadRequest("ID in path does not match ID in body.");
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        });

        adminGroup.MapDelete("{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new EnglishPartner.Modules.Curriculum.Application.Grammars.Commands.DeleteGrammar.DeleteGrammarCommand(id));
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        });
    }
}
