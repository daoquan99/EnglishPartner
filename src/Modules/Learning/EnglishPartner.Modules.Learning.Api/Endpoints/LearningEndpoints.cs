using EnglishPartner.Modules.Learning.Application.ItemMasteries.Commands.ReviewItem;
using EnglishPartner.Modules.Learning.Application.ItemMasteries.Queries.GetDueReviews;
using EnglishPartner.Modules.Learning.Application.StudySessions.Commands.PingSession;
using EnglishPartner.Modules.Learning.Application.StudySessions.Commands.StartSession;
using EnglishPartner.Modules.Learning.Application.StudySessions.Commands.SubmitAnswer;
using EnglishPartner.Modules.Learning.Application.StudySessions.Commands.SubmitSession;
using EnglishPartner.Modules.Learning.Application.UserProfiles.Queries.GetDashboard;
using EnglishPartner.Modules.Learning.Application.UserProfiles.Queries.GetLeaderboard;
using EnglishPartner.Modules.Learning.Domain.ItemMasteries;
using EnglishPartner.SharedKernel.Extensions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnglishPartner.Modules.Learning.Api.Endpoints;

internal static class LearningEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("api/v1/learning")
            .WithTags("Learning")
            .RequireAuthorization();

        // ── Dashboard ──
        group.MapGet("dashboard", async (ISender sender, HttpContext ctx) =>
        {
            if (!ctx.TryGetUserId(out var userId)) return Results.Unauthorized();
            var result = await sender.Send(new GetUserDashboardQuery(userId));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetDashboard")
        .WithSummary("Get the current user's learning dashboard: XP, streak, mastery stats.");

        // ── Leaderboard ──
        group.MapGet("leaderboard", async (ISender sender, int count = 50) =>
        {
            var result = await sender.Send(new GetLeaderboardQuery(count));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetLeaderboard")
        .WithSummary("Get the top learners leaderboard.");

        // ── Spaced Repetition ──
        group.MapGet("reviews/due", async (ISender sender, HttpContext ctx) =>
        {
            if (!ctx.TryGetUserId(out var userId)) return Results.Unauthorized();
            var result = await sender.Send(new GetDueReviewsQuery(userId, DateTime.UtcNow));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("GetDueReviews")
        .WithSummary("Get all curriculum items due for spaced repetition review today.");

        group.MapPost("reviews", async (ReviewItemRequest request, ISender sender, HttpContext ctx) =>
        {
            if (!ctx.TryGetUserId(out var userId)) return Results.Unauthorized();
            var command = new ReviewItemCommand(userId, request.ItemId, request.Type, request.QualityRating);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .WithName("ReviewItem")
        .WithSummary("Submit a standalone review for an item (SM-2 algorithm). Use when not inside a study session.");

        // ── Study Sessions ──
        group.MapPost("sessions", async (StartSessionRequest request, ISender sender, HttpContext ctx) =>
        {
            if (!ctx.TryGetUserId(out var userId)) return Results.Unauthorized();
            var command = new StartStudySessionCommand(userId, request.SessionTitle, request.TotalItems);
            var result = await sender.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/v1/learning/sessions/{result.Value}", result.Value)
                : result.ToProblemDetails();
        })
        .WithName("StartStudySession")
        .WithSummary("Start a new study session. Automatically abandons any stale sessions older than 30 min.");

        group.MapPut("sessions/{id:guid}/submit", async (Guid id, SubmitSessionRequest request, ISender sender, HttpContext ctx) =>
        {
            if (!ctx.TryGetUserId(out var userId)) return Results.Unauthorized();
            // BUG-4 fix: pass UserId so handler validates ownership
            var command = new SubmitStudySessionResultCommand(id, userId, request.Score);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .WithName("SubmitStudySession")
        .WithSummary("Complete a study session and record the final score.");

        group.MapPost("sessions/{id:guid}/ping", async (Guid id, ISender sender, HttpContext ctx) =>
        {
            if (!ctx.TryGetUserId(out var userId)) return Results.Unauthorized();
            var result = await sender.Send(new PingSessionCommand(id, userId));
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        })
        .WithName("PingStudySession")
        .WithSummary("Keep-alive ping for an active study session. Prevents it from being marked stale.");

        group.MapPost("sessions/{id:guid}/answer", async (Guid id, SubmitAnswerRequest request, ISender sender, HttpContext ctx) =>
        {
            if (!ctx.TryGetUserId(out var userId)) return Results.Unauthorized();
            var command = new SubmitAnswerCommand(
                userId, id, request.ItemId, request.ItemType, request.UserAnswer, request.TimeSpentSeconds);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        })
        .WithName("SubmitAnswer")
        .WithSummary("Submit an answer. AI evaluates it server-side (SM-2 score computed automatically).");
    }
}

public sealed record ReviewItemRequest(Guid ItemId, ItemType Type, int QualityRating);
public sealed record StartSessionRequest(string SessionTitle, int TotalItems);
public sealed record SubmitSessionRequest(int Score);

/// <summary>Answer submission body. QualityScore is now computed server-side by AI — do not send it.</summary>
public sealed record SubmitAnswerRequest(
    Guid ItemId,
    ItemType ItemType,
    string UserAnswer,
    int TimeSpentSeconds);
