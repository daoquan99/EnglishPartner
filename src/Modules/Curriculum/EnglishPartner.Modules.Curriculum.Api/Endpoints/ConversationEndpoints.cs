using EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.CreateConversation;
using EnglishPartner.Modules.Curriculum.Application.Conversations.Queries.GetConversationById;
using EnglishPartner.Modules.Curriculum.Application.Conversations.Queries.GetConversations;
using EnglishPartner.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnglishPartner.Modules.Curriculum.Api.Endpoints;

internal static class ConversationEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/curriculum/conversations")
            .WithTags("Conversations")
            .RequireAuthorization();

        var adminGroup = app.MapGroup("api/v1/admin/curriculum/conversations")
            .WithTags("Admin: Conversations")
            .RequireAuthorization("Admin");

        // ── User Endpoints ──
        group.MapGet("", async (string languageCode, ISender sender) =>
        {
            var result = await sender.Send(new GetConversationsQuery(languageCode));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        });

        group.MapGet("{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetConversationByIdQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        });

        // ── Admin Endpoints ──
        adminGroup.MapPost("", async (CreateConversationCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Created($"/api/v1/curriculum/conversations/{result.Value}", result.Value) : result.ToProblemDetails();
        });

        adminGroup.MapPut("{id:guid}", async (Guid id, EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.UpdateConversation.UpdateConversationCommand command, ISender sender) =>
        {
            if (id != command.Id) return Results.BadRequest();
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        });

        adminGroup.MapDelete("{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.DeleteConversation.DeleteConversationCommand(id));
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        });

        adminGroup.MapPost("{id:guid}/lines", async (Guid id, EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.AddConversationLine.AddConversationLineCommand command, ISender sender) =>
        {
            if (id != command.ConversationId) return Results.BadRequest();
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        });
    }
}
