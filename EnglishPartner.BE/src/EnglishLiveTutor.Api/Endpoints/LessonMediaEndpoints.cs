using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Features.Lessons.Commands.MediaOperations;
using EnglishLiveTutor.Application.Features.Lessons.Commands.RolePlayOperations;
using EnglishLiveTutor.Application.Features.Lessons.Queries.MediaQueryOperations;
using EnglishLiveTutor.Application.Features.Lessons.Queries.RolePlayQueries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EnglishLiveTutor.Api.Endpoints;

public static class LessonMediaEndpoints
{
    public static IEndpointRouteBuilder MapLessonMediaEndpoints(this IEndpointRouteBuilder app)
    {
        // 1. Upload Media File
        app.MapPost("/api/lesson-media/upload", async (
            IFormFile? file,
            [FromHeader(Name = "X-Operation-Key")] string? operationKey,
            ISender sender,
            CancellationToken ct) =>
        {
            if (file == null || file.Length == 0)
                return Results.BadRequest(new { error = "No file uploaded or file is empty." });

            try
            {
                await using var stream = file.OpenReadStream();
                string contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;

                var result = await sender.Send(new UploadLessonMediaCommand(
                    Stream: stream,
                    FileName: file.FileName,
                    ContentType: contentType,
                    FileSizeBytes: file.Length,
                    OperationKey: operationKey), ct);

                return Results.Ok(new
                {
                    assetId = result.AssetId,
                    fileName = result.FileName,
                    contentType = result.ContentType,
                    fileSizeBytes = result.FileSizeBytes,
                    url = result.Url
                });
            }
            catch (ForbiddenException)
            {
                return Results.Forbid();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization().DisableAntiforgery();

        // 2. Get Media Metadata
        app.MapGet("/api/lesson-media/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var media = await sender.Send(new GetLessonMediaQuery(id), ct);
                return Results.Ok(new
                {
                    assetId = media.AssetId,
                    contentType = media.ContentType,
                    fileSizeBytes = media.FileSizeBytes,
                    storageKey = media.StorageKey,
                    streamUrl = media.StreamUrl
                });
            }
            catch (NotFoundException)
            {
                return Results.NotFound(new { error = "Media asset not found." });
            }
            catch (ForbiddenException)
            {
                return Results.NotFound(new { error = "Media asset not found." });
            }
        }).RequireAuthorization();

        // 3. Stream Media Content
        app.MapGet("/api/lesson-media/{id:guid}/stream", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var streamResult = await sender.Send(new GetLessonMediaStreamQuery(id), ct);
                return Results.Stream(
                    stream: streamResult.Stream,
                    contentType: streamResult.ContentType,
                    fileDownloadName: streamResult.FileName,
                    enableRangeProcessing: true);
            }
            catch (NotFoundException)
            {
                return Results.NotFound(new { error = "Media asset not found." });
            }
            catch (ForbiddenException)
            {
                return Results.NotFound(new { error = "Media asset not found." });
            }
        }).RequireAuthorization();

        // 4. Role-Play: Start Session
        app.MapPost("/api/lesson-roleplay/sessions", async (
            RolePlayStartSessionRequest req,
            [FromHeader(Name = "X-Operation-Key")] string? operationKey,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var session = await sender.Send(new StartRolePlaySessionCommand(
                    Scenario: req.Scenario,
                    AiRole: req.AiRole,
                    InitialGreeting: req.InitialGreeting,
                    MaxTurns: req.MaxTurns,
                    OperationKey: operationKey), ct);

                return Results.Created($"/api/lesson-roleplay/sessions/{session.SessionId}", new
                {
                    sessionId = session.SessionId,
                    status = session.Status,
                    currentTurnIndex = session.CurrentTurnIndex,
                    aiRole = session.AiRole,
                    initialMessage = session.InitialMessage,
                    maxTurns = session.MaxTurns
                });
            }
            catch (ConflictException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization().RequirePermission("lesson-exercises.attempt");

        // 5. Role-Play: Get Session Details (Resume after restart)
        app.MapGet("/api/lesson-roleplay/sessions/{sessionId:guid}", async (
            Guid sessionId,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var session = await sender.Send(new GetRolePlaySessionQuery(sessionId), ct);
                return Results.Ok(new
                {
                    sessionId = session.SessionId,
                    status = session.Status,
                    currentTurnIndex = session.CurrentTurnIndex,
                    aiRole = session.AiRole,
                    scenario = session.Scenario,
                    maxTurns = session.MaxTurns,
                    transcript = session.Transcript.Select(t => new { role = t.Role, message = t.Message })
                });
            }
            catch (NotFoundException)
            {
                return Results.NotFound(new { error = "Role-play session not found." });
            }
        }).RequireAuthorization().RequirePermission("lesson-exercises.attempt");

        // 6. Role-Play: Send Turn
        app.MapPost("/api/lesson-roleplay/sessions/{sessionId:guid}/turns", async (
            Guid sessionId,
            [FromHeader(Name = "X-Operation-Key")] string? operationKey,
            RolePlayTurnRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var result = await sender.Send(new SendRolePlayTurnCommand(
                    SessionId: sessionId,
                    ClientTurnIndex: req.ClientTurnIndex,
                    UserMessage: req.UserMessage,
                    UserAudioAssetId: req.UserAudioAssetId,
                    OperationKey: operationKey), ct);

                return Results.Ok(new
                {
                    sessionId = result.SessionId,
                    turnIndex = result.TurnIndex,
                    userTurn = new { message = result.UserTurn.Message },
                    aiTurn = new { message = result.AiTurn.Message },
                    isCompleted = result.IsCompleted,
                    remainingTurns = result.RemainingTurns
                });
            }
            catch (NotFoundException)
            {
                return Results.NotFound(new { error = "Role-play session not found." });
            }
            catch (ConflictException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization().RequirePermission("lesson-exercises.attempt");

        // 7. Role-Play: End Session
        app.MapPost("/api/lesson-roleplay/sessions/{sessionId:guid}/end", async (
            Guid sessionId,
            [FromHeader(Name = "X-Operation-Key")] string? operationKey,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var result = await sender.Send(new EndRolePlaySessionCommand(sessionId, operationKey), ct);
                return Results.Ok(new
                {
                    sessionId = result.SessionId,
                    status = result.Status,
                    totalTurnsCompleted = result.TotalTurnsCompleted,
                    transcript = result.Transcript.Select(h => new { role = h.Role, message = h.Message }),
                    message = result.Message
                });
            }
            catch (NotFoundException)
            {
                return Results.NotFound(new { error = "Role-play session not found." });
            }
            catch (ConflictException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        }).RequireAuthorization().RequirePermission("lesson-exercises.attempt");

        return app;
    }
}

public sealed record RolePlayStartSessionRequest(
    string Scenario,
    string? AiRole,
    string? InitialGreeting,
    int MaxTurns = 6
);

public sealed record RolePlayTurnRequest(
    int ClientTurnIndex,
    string UserMessage,
    string? UserAudioAssetId = null
);
