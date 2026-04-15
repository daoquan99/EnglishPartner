using EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.AddConversationLine;
using EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.CreateConversation;
using EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.DeleteConversation;
using EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.UpdateConversation;
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
        })
        .WithName("GetConversations")
        .WithSummary("Get all conversations for a given language code.");

        group.MapGet("{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new GetConversationByIdQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetConversationById")
        .WithSummary("Get a conversation with all its lines and translations.");

        // ── Admin Endpoints ──
        adminGroup.MapPost("", async (CreateConversationCommand command, ISender sender) =>
        {
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/v1/curriculum/conversations/{result.Value}", result.Value)
                : result.ToProblemDetails();
        })
        .WithName("CreateConversation")
        .WithSummary("Create a new conversation.")
        .RequireAuthorization("curriculum.conversation.write");

        adminGroup.MapPut("{id:guid}", async (Guid id, UpdateConversationCommand command, ISender sender) =>
        {
            if (id != command.Id) return Results.BadRequest("Route ID does not match body ID.");
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .WithName("UpdateConversation")
        .WithSummary("Update an existing conversation's metadata.")
        .RequireAuthorization("curriculum.conversation.write");

        adminGroup.MapDelete("{id:guid}", async (Guid id, ISender sender) =>
        {
            var result = await sender.Send(new DeleteConversationCommand(id));
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .WithName("DeleteConversation")
        .WithSummary("Soft-delete a conversation.")
        .RequireAuthorization("curriculum.conversation.write");

        // BUG-3 fix: ConversationId injected from route, not from body
        adminGroup.MapPost("{conversationId:guid}/lines",
            async (Guid conversationId, AddConversationLineRequest request, ISender sender) =>
            {
                var command = new AddConversationLineCommand(
                    conversationId,
                    request.Speaker,
                    request.Text,
                    request.Translation,
                    request.AudioUrl);
                var result = await sender.Send(command);
                return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
            })
        .WithName("AddConversationLine")
        .WithSummary("Add a new line to a conversation.")
        .RequireAuthorization("curriculum.conversation.write");
    }
}

/// <summary>Request body for adding a conversation line. ConversationId comes from the route.</summary>
public sealed record AddConversationLineRequest(
    string Speaker,
    string Text,
    string? Translation,
    string? AudioUrl);
