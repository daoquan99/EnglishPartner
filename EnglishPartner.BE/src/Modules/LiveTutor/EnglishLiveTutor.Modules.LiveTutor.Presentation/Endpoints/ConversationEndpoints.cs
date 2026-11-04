using EnglishLiveTutor.Api.Common;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Conversations.Commands.CreateConversation;
using EnglishLiveTutor.Application.Features.Conversations.Commands.DeleteConversation;
using EnglishLiveTutor.Application.Features.Conversations.Commands.UpdateConversationSettings;
using EnglishLiveTutor.Application.Features.Conversations.Queries.GetConversationHistory;
using EnglishLiveTutor.Application.Features.Conversations.Queries.ListConversations;
using EnglishLiveTutor.Application.Features.Conversations.Queries.PreviewSystemPrompt;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnglishLiveTutor.Api.Endpoints;

public static class ConversationEndpoints
{
    public static IEndpointRouteBuilder MapConversationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/conversations").RequireAuthorization();

        group.MapGet("/", async (
            HttpContext httpContext,
            string? search,
            int? page,
            int? pageSize,
            bool? paged,
            ISender sender,
            CancellationToken ct) =>
        {
            var pagedConversations = await sender.Send(new ListConversationsQuery(search, page, pageSize), ct);
            return pagedConversations.ToHttpResult(httpContext, paged);
        });

        group.MapGet("/{id}", async (
            string id,
            ISender sender,
            CancellationToken ct) =>
        {
            var conv = await sender.Send(new GetConversationHistoryQuery(id), ct);
            return conv is null ? ApiProblem.NotFound("Không tìm thấy cuộc hội thoại.") : Results.Ok(conv);
        });

        group.MapPost("/", async (
            CreateConversationRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var conv = await sender.Send(new CreateConversationCommand(req), ct);
            return Results.Ok(conv);
        });

        group.MapPut("/{id}/settings", async (
            string id,
            UpdateConversationSettingsRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var conv = await sender.Send(new UpdateConversationSettingsCommand(id, req), ct);
            return conv is null ? ApiProblem.NotFound("Không tìm thấy cuộc hội thoại.") : Results.Ok(conv);
        });

        group.MapDelete("/{id}", async (
            string id,
            ISender sender,
            CancellationToken ct) =>
        {
            bool deleted = await sender.Send(new DeleteConversationCommand(id), ct);
            return deleted ? Results.NoContent() : ApiProblem.NotFound("Không tìm thấy cuộc hội thoại.");
        });

        var promptPreviewGroup = app.MapGroup("/api/prompts").RequireAuthorization();

        promptPreviewGroup.MapPost("/preview", async (
            BuildSystemPromptPreviewRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            string systemInstruction = await sender.Send(new PreviewSystemPromptQuery(req), ct);
            return Results.Ok(new
            {
                req.CefrLevel,
                req.ConversationMode,
                req.VoiceName,
                req.SpeechSpeed,
                req.ExtendedThinking,
                systemInstruction
            });
        });

        return app;
    }
}
