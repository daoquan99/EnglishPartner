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
                return Results.Unauthorized();
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
                return Results.Unauthorized();
            }

            try
            {
                var updated = await sender.Send(
                    new UpdateLearningSettingsCommand(currentUser.UserId!, req),
                    ct);
                return Results.Ok(updated);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
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
                return Results.Unauthorized();
            }

            string userId = currentUser.UserId!;
            var plan = await sender.Send(
                new GetDailyVocabularyPlanQuery(userId, timeZoneId, null),
                ct);

            if (plan is null)
            {
                return Results.NotFound(new
                {
                    code = "daily_plan_not_initialized",
                    error = "daily_plan_not_initialized",
                    message = "Kế hoạch học tập từ vựng hôm nay chưa được khởi tạo."
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
                return Results.Unauthorized();
            }

            try
            {
                string userId = currentUser.UserId!;
                var plan = await sender.Send(
                    new InitializeDailyVocabularyPlanCommand(userId, req ?? new InitializeDailyPlanRequest()),
                    ct);
                return Results.Ok(plan);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
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
                return Results.Unauthorized();
            }

            try
            {
                string userId = currentUser.UserId!;
                var updatedCard = await sender.Send(
                    new SubmitVocabularyReviewCommand(userId, itemId, req),
                    ct);
                return Results.Ok(updatedCard);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapPost("/review", async (
            SubmitVocabularyReviewRequest req,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(currentUser.UserId))
            {
                return Results.Unauthorized();
            }

            try
            {
                string userId = currentUser.UserId!;
                var updatedCard = await sender.Send(
                    new SubmitVocabularyReviewCommand(userId, null, req),
                    ct);
                return Results.Ok(updatedCard);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapGet("/progress", async (
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(currentUser.UserId))
            {
                return Results.Unauthorized();
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
                return Results.Unauthorized();
            }

            string userId = currentUser.UserId!;
            var history = await sender.Send(new GetLearningHistoryQuery(userId, limit ?? 30), ct);
            return Results.Ok(history);
        });

        return app;
    }
}
