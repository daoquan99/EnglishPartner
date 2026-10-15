using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Learning.Commands.InitializeDailyVocabularyPlan;
using EnglishLiveTutor.Application.Features.Learning.Commands.SubmitVocabularyReview;
using EnglishLiveTutor.Application.Features.Learning.Queries.GetDailyVocabularyPlan;
using EnglishLiveTutor.Application.Features.Learning.Queries.GetLearningHistory;
using EnglishLiveTutor.Application.Features.Learning.Queries.GetLearningProgress;

namespace EnglishLiveTutor.Api.Endpoints;

public static class DailyLearningEndpoints
{
    public static IEndpointRouteBuilder MapDailyLearningEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/learning/flashcards");

        group.MapGet("/today", async (
            string? timeZoneId,
            int? target,
            IQueryHandler<GetDailyVocabularyPlanQuery, DailyVocabularyPlanResponseDto> handler,
            CancellationToken ct) =>
        {
            var plan = await handler.HandleAsync(
                new GetDailyVocabularyPlanQuery("default-learner", timeZoneId, target),
                ct);
            return Results.Ok(plan);
        });

        group.MapPost("/today/initialize", async (
            InitializeDailyPlanRequest req,
            ICommandHandler<InitializeDailyVocabularyPlanCommand, DailyVocabularyPlanResponseDto> handler,
            CancellationToken ct) =>
        {
            try
            {
                var plan = await handler.HandleAsync(
                    new InitializeDailyVocabularyPlanCommand("default-learner", req),
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
            ICommandHandler<SubmitVocabularyReviewCommand, VocabularyFlashcardDto> handler,
            CancellationToken ct) =>
        {
            try
            {
                var updatedCard = await handler.HandleAsync(
                    new SubmitVocabularyReviewCommand("default-learner", itemId, req),
                    ct);
                return Results.Ok(updatedCard);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapGet("/progress", async (
            IQueryHandler<GetLearningProgressQuery, object> handler,
            CancellationToken ct) =>
        {
            var summary = await handler.HandleAsync(new GetLearningProgressQuery("default-learner"), ct);
            return Results.Ok(summary);
        });

        group.MapGet("/history", async (
            int? limit,
            IQueryHandler<GetLearningHistoryQuery, object> handler,
            CancellationToken ct) =>
        {
            var history = await handler.HandleAsync(new GetLearningHistoryQuery("default-learner", limit ?? 30), ct);
            return Results.Ok(history);
        });

        return app;
    }
}
