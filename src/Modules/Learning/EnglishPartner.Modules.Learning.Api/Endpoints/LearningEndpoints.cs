using EnglishPartner.Modules.Learning.Application.ItemMasteries.Commands.ReviewItem;
using EnglishPartner.Modules.Learning.Application.ItemMasteries.Queries.GetDueReviews;
using EnglishPartner.Modules.Learning.Application.StudySessions.Commands.StartSession;
using EnglishPartner.Modules.Learning.Application.StudySessions.Commands.SubmitSession;
using EnglishPartner.Modules.Learning.Application.UserProfiles.Queries.GetDashboard;
using EnglishPartner.Modules.Learning.Application.UserProfiles.Queries.GetLeaderboard;
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
        group.MapGet("dashboard", async (ISender sender, HttpContext context) =>
        {
            var userIdStr = context.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();

            var result = await sender.Send(new GetUserDashboardQuery(userId));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        });

        // ── Leaderboard ──
        group.MapGet("leaderboard", async (ISender sender, int count = 50) =>
        {
            var result = await sender.Send(new GetLeaderboardQuery(count));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        });

        // ── Spaced Repetition (Reviews) ──
        group.MapGet("reviews/due", async (ISender sender, HttpContext context) =>
        {
            var userIdStr = context.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();

            // Default to getting things due on or before today
            var result = await sender.Send(new GetDueReviewsQuery(userId, DateTime.UtcNow));
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        });

        group.MapPost("reviews", async (ReviewItemRequest request, ISender sender, HttpContext context) =>
        {
            var userIdStr = context.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();

            var command = new ReviewItemCommand(userId, request.ItemId, request.Type, request.QualityRating);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        });

        // ── Study Sessions ──
        group.MapPost("sessions", async (StartSessionRequest request, ISender sender, HttpContext context) =>
        {
            var userIdStr = context.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();

            var command = new StartStudySessionCommand(userId, request.SessionTitle, request.TotalItems);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Created($"/api/v1/learning/sessions/{result.Value}", result.Value) : result.ToProblemDetails();
        });

        group.MapPut("sessions/{id:guid}/submit", async (Guid id, SubmitSessionRequest request, ISender sender) =>
        {
            var command = new SubmitStudySessionResultCommand(id, request.Score);
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        });

        group.MapPost("sessions/{id:guid}/ping", async (Guid id, ISender sender, HttpContext context) =>
        {
            var userIdStr = context.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();

            var result = await sender.Send(new EnglishPartner.Modules.Learning.Application.StudySessions.Commands.PingSession.PingSessionCommand(id, userId));
            return result.IsSuccess ? Results.NoContent() : result.ToProblemDetails();
        });

        group.MapPost("sessions/{id:guid}/answer", async (Guid id, SubmitAnswerRequest request, ISender sender, HttpContext context) =>
        {
            var userIdStr = context.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();

            var command = new EnglishPartner.Modules.Learning.Application.StudySessions.Commands.SubmitAnswer.SubmitAnswerCommand(
                userId, id, request.ItemId, request.ItemType, request.UserAnswer, request.QualityScore, request.TimeSpentSeconds);
                
            var result = await sender.Send(command);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblemDetails();
        });
    }
}

public sealed record ReviewItemRequest(Guid ItemId, EnglishPartner.Modules.Learning.Domain.ItemMasteries.ItemType Type, int QualityRating);
public sealed record StartSessionRequest(string SessionTitle, int TotalItems);
public sealed record SubmitSessionRequest(int Score);

/// <param name="QualityScore">0-5 SM-2 quality rating. For AI-evaluated items, pass the score returned by POST /api/v1/ai/evaluate.</param>
public sealed record SubmitAnswerRequest(
    Guid ItemId,
    EnglishPartner.Modules.Learning.Domain.ItemMasteries.ItemType ItemType,
    string UserAnswer,
    int QualityScore,
    int TimeSpentSeconds);
