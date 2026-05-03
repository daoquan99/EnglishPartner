using EnglishPartner.Modules.AI.Application.Chat.Commands.SendMessage;
using EnglishPartner.Modules.AI.Application.Chat.Commands.StartChat;
using EnglishPartner.Modules.AI.Application.Chat.Queries.GetChatHistory;
using EnglishPartner.Modules.AI.Application.Chat.Queries.GetMySessions;
using EnglishPartner.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnglishPartner.Modules.AI.Api.Endpoints;

internal static class ChatEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/chat/sessions", async (ISender sender) =>
        {
            var result = await sender.Send(new GetMySessionsQuery());
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetMyChatSessions")
        .WithSummary("List all AI Tutor chat sessions for the current user.");

        group.MapPost("/chat/sessions", async ([Microsoft.AspNetCore.Mvc.FromBody] StartChatRequest request, ISender sender) =>
        {
            var command = new StartChatCommand(request.Title, request.SystemPrompt, request.Topic);
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/v1/ai/chat/sessions/{result.Value}", new { SessionId = result.Value })
                : result.ToProblemDetails();
        })
        .WithName("StartChat")
        .WithSummary("Start a new AI Tutor chat session.");

        group.MapGet("/chat/sessions/{sessionId:guid}/messages", async (Guid sessionId, ISender sender) =>
        {
            var result = await sender.Send(new GetChatHistoryQuery(sessionId));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetChatHistory")
        .WithSummary("Get all messages in a chat session.");

        group.MapPost("/chat/sessions/{sessionId:guid}/messages", 
            async (Guid sessionId, [Microsoft.AspNetCore.Mvc.FromBody] SendMessageRequest request, ISender sender) =>
        {
            var command = new SendMessageCommand(sessionId, request.Text);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("SendChatMessage")
        .WithSummary("Send a message to the AI Tutor and get a reply.")
        .RequireRateLimiting("ai-chat");

        group.MapPatch("/chat/sessions/{sessionId:guid}/messages/{messageId:guid}/audio-url", 
            async (Guid sessionId, Guid messageId, [Microsoft.AspNetCore.Mvc.FromBody] UpdateMessageAudioUrlRequest request, ISender sender) =>
        {
            var command = new EnglishPartner.Modules.AI.Application.Chat.Commands.UpdateMessageAudioUrl.UpdateMessageAudioUrlCommand(sessionId, messageId, request.AudioUrl);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
        })
        .WithName("UpdateMessageAudioUrl")
        .WithSummary("Update the audio URL of a chat message.");

        group.MapPatch("/chat/sessions/{sessionId:guid}/messages/{messageId:guid}/translation", async (Guid sessionId, Guid messageId, [Microsoft.AspNetCore.Mvc.FromBody] UpdateMessageTranslationRequest request, ISender sender) =>
        {
            var command = new EnglishPartner.Modules.AI.Application.Chat.Commands.UpdateMessageTranslation.UpdateMessageTranslationCommand(sessionId, messageId, request.Translation);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok() : result.ToProblemDetails();
        })
        .WithName("UpdateMessageTranslation")
        .WithSummary("Update the translation of a chat message.");

        group.MapPost("/chat/sessions/{sessionId:guid}/messages/{messageId:guid}/audio", async (
            Guid sessionId, 
            Guid messageId, 
            IFormFile audioFile, 
            ISender sender) =>
        {
            if (audioFile is null || audioFile.Length == 0)
                return Results.BadRequest("Audio file is required.");

            const long MaxAudioBytes = 10 * 1024 * 1024; // 10 MB
            if (audioFile.Length > MaxAudioBytes)
                return Results.BadRequest("Audio file is too large. Maximum size is 10 MB.");

            using var stream = audioFile.OpenReadStream();
            var command = new EnglishPartner.Modules.AI.Application.Chat.Commands.UploadMessageAudio.UploadMessageAudioCommand(
                sessionId,
                messageId,
                stream,
                audioFile.FileName,
                audioFile.ContentType);

            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("UploadMessageAudio")
        .WithSummary("Upload an audio file for a message and automatically save its URL.")
        .DisableAntiforgery();
    }
}

public sealed record StartChatRequest(string Title, string? SystemPrompt = null, string? Topic = null);
public sealed record SendMessageRequest(string Text);
public sealed record UpdateMessageAudioUrlRequest(string AudioUrl);
public sealed record UpdateMessageTranslationRequest(string Translation);
