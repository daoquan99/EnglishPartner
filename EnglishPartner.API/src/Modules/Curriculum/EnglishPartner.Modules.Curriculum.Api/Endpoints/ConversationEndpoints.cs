using EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.AddConversationLine;
using EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.CreateConversation;
using EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.DeleteConversation;
using EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.GenerateAllLineAudio;
using EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.GenerateLineAudio;
using EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.ReorderConversationLines;
using EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.UpdateConversation;
using EnglishPartner.Modules.Curriculum.Application.Conversations.Commands.UpdateConversationLine;
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
        group.MapGet("", async (ISender sender, string languageCode = "en", int page = 1, int pageSize = 20, Guid? topicId = null, string? level = null) =>
        {
            var result = await sender.Send(new GetConversationsQuery(languageCode, page, pageSize, topicId, level ));
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
                    request.Translations,
                    request.AudioUrl);
                var result = await sender.Send(command);
                return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
            })
        .WithName("AddConversationLine")
        .WithSummary("Add a new line to a conversation.")
        .RequireAuthorization("curriculum.conversation.write");

        adminGroup.MapPut("{conversationId:guid}/lines/{lineId:guid}",
            async (Guid conversationId, Guid lineId, UpdateConversationLineRequest request, ISender sender) =>
            {
                var command = new UpdateConversationLineCommand(
                    conversationId,
                    lineId,
                    request.Speaker,
                    request.Text,
                    request.AudioUrl,
                    request.Translations);
                var result = await sender.Send(command);
                return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
            })
        .WithName("UpdateConversationLine")
        .WithSummary("Update a line in a conversation.")
        .RequireAuthorization("curriculum.conversation.write");

        adminGroup.MapPut("{conversationId:guid}/lines/reorder",
            async (Guid conversationId, ReorderConversationLinesRequest request, ISender sender) =>
            {
                var command = new ReorderConversationLinesCommand(conversationId, request.OrderedLineIds);
                var result = await sender.Send(command);
                return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
            })
        .WithName("ReorderConversationLines")
        .WithSummary("Reorder lines in a conversation.")
        .RequireAuthorization("curriculum.conversation.write");

        adminGroup.MapPost("{conversationId:guid}/lines/{lineId:guid}/generate-audio",
            async (Guid conversationId, Guid lineId, GenerateLineAudioRequest? request, ISender sender) =>
            {
                var command = new GenerateLineAudioCommand(conversationId, lineId, request?.Voice);
                var result = await sender.Send(command);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
            })
        .WithName("GenerateLineAudio")
        .WithSummary("Generate TTS audio for a single conversation line and save the audioUrl.")
        .RequireAuthorization("curriculum.conversation.write")
        .RequireRateLimiting("ai-scoring");

        adminGroup.MapPost("{conversationId:guid}/generate-all-audio",
            async (Guid conversationId, GenerateAllLineAudioRequest? request, ISender sender) =>
            {
                var command = new GenerateAllLineAudioCommand(conversationId, request?.Voice);
                var result = await sender.Send(command);
                return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
            })
        .WithName("GenerateAllLineAudio")
        .WithSummary("Generate TTS audio for all lines missing audioUrl in a conversation.")
        .RequireAuthorization("curriculum.conversation.write")
        .RequireRateLimiting("ai-scoring");
    }
}

/// <summary>Request body for reordering conversation lines.</summary>
public sealed record ReorderConversationLinesRequest(List<Guid> OrderedLineIds);

/// <summary>Request body for adding a conversation line. ConversationId comes from the route.</summary>
public sealed record AddConversationLineRequest(
    string Speaker,
    string Text,
    Dictionary<string, string>? Translations,
    string? AudioUrl);

/// <summary>Request body for updating a conversation line. IDs come from the route.</summary>
public sealed record UpdateConversationLineRequest(
    string Speaker,
    string Text,
    string? AudioUrl,
    Dictionary<string, string>? Translations);

public sealed record GenerateLineAudioRequest(string? Voice = null);
public sealed record GenerateAllLineAudioRequest(string? Voice = null);
