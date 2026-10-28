using EnglishLiveTutor.Api.Common;
using EnglishLiveTutor.Application.Features.Lessons.Commands.SynthesizeExerciseAudio;
using EnglishLiveTutor.Application.Features.VocabularyGames.Commands.AbandonFallingWordSession;
using EnglishLiveTutor.Application.Features.VocabularyGames.Commands.CreateFallingWordSession;
using EnglishLiveTutor.Application.Features.VocabularyGames.Commands.SkipFallingWordRound;
using EnglishLiveTutor.Application.Features.VocabularyGames.Commands.StartNextFallingWordRound;
using EnglishLiveTutor.Application.Features.VocabularyGames.Commands.SubmitFallingWordAnswer;
using EnglishLiveTutor.Application.Features.VocabularyGames.Commands.SynthesizeFallingWordAudio;
using EnglishLiveTutor.Application.Features.VocabularyGames.Commands.TimeoutFallingWordRound;
using EnglishLiveTutor.Application.Features.VocabularyGames.Contracts;
using EnglishLiveTutor.Application.Features.VocabularyGames.Exceptions;
using EnglishLiveTutor.Application.Features.VocabularyGames.Queries.GetFallingWordHistory;
using EnglishLiveTutor.Application.Features.VocabularyGames.Queries.GetFallingWordPool;
using EnglishLiveTutor.Application.Features.VocabularyGames.Queries.GetFallingWordSession;
using EnglishLiveTutor.Application.Features.VocabularyGames.Queries.ListFallingWordSessionRounds;
using EnglishLiveTutor.Application.Features.VocabularyGames.Queries.ListFallingWordSessions;
using EnglishLiveTutor.Domain.VocabularyGames.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EnglishLiveTutor.Api.Endpoints;

public static class VocabularyGamesEndpoints
{
    public static IEndpointRouteBuilder MapVocabularyGamesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/vocabulary-games/falling-words")
            .RequireAuthorization();

        // 1. GET /pool - Pool counters & breakdown
        group.MapGet("/pool", async (
            [FromQuery] string? source,
            [FromQuery] string? cefrLevel,
            [FromQuery] string? playFilter,
            [FromQuery] string? timeZoneId,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var parsedSource = !string.IsNullOrWhiteSpace(source) && Enum.TryParse<FallingWordGameSource>(source, true, out var s)
                    ? s
                    : FallingWordGameSource.VocabularyCatalog;

                var parsedFilter = !string.IsNullOrWhiteSpace(playFilter) && Enum.TryParse<FallingWordPlayFilter>(playFilter, true, out var f)
                    ? f
                    : FallingWordPlayFilter.All;

                var result = await sender.Send(new GetFallingWordPoolQuery(
                    Source: parsedSource,
                    CefrLevel: cefrLevel,
                    PlayFilter: parsedFilter,
                    TimeZoneId: timeZoneId), ct);

                return Results.Ok(result);
            }
            catch (DailyPlanNotFoundException ex)
            {
                return ApiProblem.BadRequest(ex.Message, customExtensions: new Dictionary<string, object?> { ["code"] = "DailyPlanNotFound" });
            }
        });

        // 2. POST /sessions - Create new session
        group.MapPost("/sessions", async (
            [FromBody] CreateFallingWordSessionRequest req,
            [FromHeader(Name = "X-Operation-Key")] string? operationKey,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var result = await sender.Send(new CreateFallingWordSessionCommand(req, operationKey), ct);
                return Results.Created($"/api/vocabulary-games/falling-words/sessions/{result.Id}", result);
            }
            catch (DailyPlanNotFoundException ex)
            {
                return ApiProblem.BadRequest(ex.Message, customExtensions: new Dictionary<string, object?> { ["code"] = "DailyPlanNotFound" });
            }
        });

        // 3. GET /sessions - History list of sessions
        group.MapGet("/sessions", async (
            [FromQuery] int page,
            [FromQuery] int pageSize,
            [FromQuery] string? source,
            [FromQuery] string? status,
            ISender sender,
            CancellationToken ct) =>
        {
            FallingWordGameSource? parsedSource = !string.IsNullOrWhiteSpace(source) && Enum.TryParse<FallingWordGameSource>(source, true, out var s) ? s : null;
            FallingWordGameStatus? parsedStatus = !string.IsNullOrWhiteSpace(status) && Enum.TryParse<FallingWordGameStatus>(status, true, out var st) ? st : null;

            var result = await sender.Send(new ListFallingWordSessionsQuery(
                Page: page <= 0 ? 1 : page,
                PageSize: pageSize <= 0 ? 20 : pageSize,
                Source: parsedSource,
                Status: parsedStatus), ct);

            return Results.Ok(result);
        });

        // 4. GET /sessions/{id} - Get session detail / resume
        group.MapGet("/sessions/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetFallingWordSessionQuery(id), ct);
            if (result == null)
            {
                return ApiProblem.NotFound("Phiên trò chơi không tồn tại.");
            }

            return Results.Ok(result);
        });

        // 5. POST /sessions/{id}/rounds/next - Start next round
        group.MapPost("/sessions/{id:guid}/rounds/next", async (
            Guid id,
            [FromHeader(Name = "X-Operation-Key")] string? operationKey,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new StartNextFallingWordRoundCommand(id, operationKey), ct);
            if (result == null)
            {
                return Results.Ok(new { message = "Phiên chơi đã hoàn thành.", isCompleted = true });
            }

            return Results.Ok(result);
        });

        // 6. POST /sessions/{id}/rounds/{roundId}/answer - Submit answer
        group.MapPost("/sessions/{id:guid}/rounds/{roundId:guid}/answer", async (
            Guid id,
            Guid roundId,
            [FromBody] SubmitFallingWordAnswerRequest req,
            [FromHeader(Name = "X-Operation-Key")] string? operationKey,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new SubmitFallingWordAnswerCommand(id, roundId, req, operationKey), ct);
            return Results.Ok(result);
        });

        // 7. POST /sessions/{id}/rounds/{roundId}/timeout - Finalize timeout
        group.MapPost("/sessions/{id:guid}/rounds/{roundId:guid}/timeout", async (
            Guid id,
            Guid roundId,
            [FromHeader(Name = "X-Operation-Key")] string? operationKey,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var result = await sender.Send(new TimeoutFallingWordRoundCommand(id, roundId, operationKey), ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return ApiProblem.Conflict(ex.Message);
            }
        });

        // 7b. POST /sessions/{id}/rounds/{roundId}/skip - Skip round
        group.MapPost("/sessions/{id:guid}/rounds/{roundId:guid}/skip", async (
            Guid id,
            Guid roundId,
            [FromHeader(Name = "X-Operation-Key")] string? operationKey,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new SkipFallingWordRoundCommand(id, roundId, operationKey), ct);
            return Results.Ok(result);
        });

        // 7c. POST /sessions/{id}/rounds/{roundId}/synthesize - Synthesize hint audio
        group.MapPost("/sessions/{id:guid}/rounds/{roundId:guid}/synthesize", async (
            Guid id,
            Guid roundId,
            [FromBody] SynthesizeExerciseAudioRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new SynthesizeFallingWordAudioCommand(id, roundId, req), ct);
            return Results.Ok(result);
        }).RequireRateLimiting("ai");

        // 8. POST /sessions/{id}/abandon - Abandon session
        group.MapPost("/sessions/{id:guid}/abandon", async (
            Guid id,
            [FromHeader(Name = "X-Operation-Key")] string? operationKey,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new AbandonFallingWordSessionCommand(id, operationKey), ct);
            return Results.Ok(result);
        });

        // 9. GET /sessions/{id}/rounds - List rounds for a session
        group.MapGet("/sessions/{id:guid}/rounds", async (
            Guid id,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ListFallingWordSessionRoundsQuery(
                SessionId: id,
                Page: page <= 0 ? 1 : page,
                PageSize: pageSize <= 0 ? 20 : pageSize), ct);

            return Results.Ok(result);
        });

        // 10. GET /words/history - History of rounds across user/senses
        group.MapGet("/words/history", async (
            [FromQuery] Guid? senseId,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetFallingWordHistoryQuery(
                SenseId: senseId,
                Page: page <= 0 ? 1 : page,
                PageSize: pageSize <= 0 ? 20 : pageSize), ct);

            return Results.Ok(result);
        });

        return app;
    }
}
