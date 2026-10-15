using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Learning.Commands.InitializeDailyVocabularyPlan;
using EnglishLiveTutor.Application.Features.Learning.Commands.SubmitVocabularyReview;
using EnglishLiveTutor.Application.Features.Learning.Queries.GetDailyVocabularyPlan;
using EnglishLiveTutor.Application.Features.Learning.Queries.GetLearningHistory;
using EnglishLiveTutor.Application.Features.Learning.Queries.GetLearningProgress;
using MediatR;

namespace EnglishLiveTutor.Api.Endpoints;

public static class DailyLearningEndpoints
{
    public static IEndpointRouteBuilder MapDailyLearningEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/learning/flashcards").RequireAuthorization();

        group.MapGet("/today", async (
            string? timeZoneId,
            int? target,
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
                new GetDailyVocabularyPlanQuery(userId, timeZoneId, target),
                ct);

            if (plan is null)
            {
                plan = await sender.Send(
                    new InitializeDailyVocabularyPlanCommand(
                        userId,
                        new InitializeDailyPlanRequest(
                            ApiKey: null,
                            NewVocabularyTarget: target ?? 20,
                            TimeZoneId: timeZoneId ?? "Asia/Ho_Chi_Minh",
                            GenerateWithAiIfNeeded: false)),
                    ct);
            }

            return Results.Ok(plan);
        });

        group.MapPost("/today/initialize", async (
            InitializeDailyPlanRequest req,
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
                    new InitializeDailyVocabularyPlanCommand(userId, req),
                    ct);
                return Results.Ok(plan);
            }
            catch (InvalidOperationException ex)
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
