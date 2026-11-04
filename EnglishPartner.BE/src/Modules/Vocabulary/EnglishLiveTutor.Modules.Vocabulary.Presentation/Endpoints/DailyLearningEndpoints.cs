using EnglishLiveTutor.Api.Common;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Learning.Commands.InitializeDailyVocabularyPlan;
using EnglishLiveTutor.Application.Features.Learning.Commands.SubmitVocabularyReview;
using EnglishLiveTutor.Application.Features.Learning.Commands.UpdateLearningSettings;
using EnglishLiveTutor.Application.Features.Learning.Queries.GetDailyVocabularyPlan;
using EnglishLiveTutor.Application.Features.Learning.Queries.GetLearningHistory;
using EnglishLiveTutor.Application.Features.Learning.Queries.GetLearningProgress;
using EnglishLiveTutor.Application.Features.Learning.Queries.GetLearningSettings;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnglishLiveTutor.Api.Endpoints;

public static class DailyLearningEndpoints
{
    public static IEndpointRouteBuilder MapDailyLearningEndpoints(this IEndpointRouteBuilder app)
    {
        var settingsGroup = app.MapGroup("/api/learning/settings").RequireAuthorization();

        settingsGroup.MapGet("/", async (
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(currentUser.UserId))
            {
                return ApiProblem.Unauthorized();
            }

            var settings = await sender.Send(new GetLearningSettingsQuery(currentUser.UserId!), ct);
            return Results.Ok(settings);
        });

        settingsGroup.MapPut("/", async (
            UpdateLearningSettingsRequest req,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(currentUser.UserId))
            {
                return ApiProblem.Unauthorized();
            }

            var updated = await sender.Send(
                new UpdateLearningSettingsCommand(currentUser.UserId!, req),
                ct);
            return Results.Ok(updated);
        });

        var group = app.MapGroup("/api/learning/flashcards").RequireAuthorization();

        group.MapGet("/today", async (
            string? timeZoneId,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(currentUser.UserId))
            {
                return ApiProblem.Unauthorized();
            }

            string userId = currentUser.UserId!;
            var plan = await sender.Send(
                new GetDailyVocabularyPlanQuery(userId, timeZoneId, null),
                ct);

            if (plan is null)
            {
                return ApiProblem.NotFound(
                    "Kế hoạch học tập từ vựng hôm nay chưa được khởi tạo.",
                    customExtensions: new Dictionary<string, object?>
                    {
                        ["code"] = "daily_plan_not_initialized"
                    });
            }

            return Results.Ok(plan);
        });

        group.MapPost("/today/initialize", async (
            InitializeDailyPlanRequest? req,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(currentUser.UserId))
            {
                return ApiProblem.Unauthorized();
            }

            string userId = currentUser.UserId!;
            var plan = await sender.Send(
                new InitializeDailyVocabularyPlanCommand(userId, req ?? new InitializeDailyPlanRequest()),
                ct);
            return Results.Ok(plan);
        });

        group.MapPost("/{itemId:guid}/review", async (
            Guid itemId,
            SubmitVocabularyReviewRequest req,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(currentUser.UserId))
            {
                return ApiProblem.Unauthorized();
            }

            string userId = currentUser.UserId!;
            var updatedCard = await sender.Send(
                new SubmitVocabularyReviewCommand(userId, itemId, req),
                ct);
            return Results.Ok(updatedCard);
        });

        group.MapPost("/review", async (
            SubmitVocabularyReviewRequest req,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(currentUser.UserId))
            {
                return ApiProblem.Unauthorized();
            }

            string userId = currentUser.UserId!;
            var updatedCard = await sender.Send(
                new SubmitVocabularyReviewCommand(userId, null, req),
                ct);
            return Results.Ok(updatedCard);
        });

        group.MapGet("/progress", async (
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(currentUser.UserId))
            {
                return ApiProblem.Unauthorized();
            }

            string userId = currentUser.UserId!;
            var summary = await sender.Send(new GetLearningProgressQuery(userId), ct);
            return Results.Ok(summary);
        });

        group.MapGet("/history", async (
            int? limit,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(currentUser.UserId))
            {
                return ApiProblem.Unauthorized();
            }

            string userId = currentUser.UserId!;
            var history = await sender.Send(new GetLearningHistoryQuery(userId, limit ?? 30), ct);
            return Results.Ok(history);
        });

        return app;
    }
}
