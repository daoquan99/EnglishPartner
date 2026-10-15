using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Pronunciation.Commands.AssessPronunciation;
using EnglishLiveTutor.Application.Features.Pronunciation.Commands.UpdateScoringRubric;
using EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetActiveScoringRubric;
using EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetPronunciationHistory;
using EnglishLiveTutor.Application.Features.Pronunciation.Queries.GetPronunciationModels;
using EnglishLiveTutor.Domain.AiCatalog;
using EnglishLiveTutor.Domain.Pronunciation;

namespace EnglishLiveTutor.Api.Endpoints;

public static class PronunciationEndpoints
{
    public static IEndpointRouteBuilder MapPronunciationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pronunciation");

        group.MapGet("/models", async (
            IQueryHandler<GetPronunciationModelsQuery, List<AiModelConfigEntity>> handler,
            CancellationToken ct) =>
        {
            var models = await handler.HandleAsync(new GetPronunciationModelsQuery(), ct);
            return Results.Ok(models);
        });

        group.MapGet("/rubrics/active", async (
            IQueryHandler<GetActiveScoringRubricQuery, ScoringRubricEntity?> handler,
            CancellationToken ct) =>
        {
            var rubric = await handler.HandleAsync(new GetActiveScoringRubricQuery(), ct);
            return Results.Ok(rubric);
        });

        group.MapPost("/assessments", async (
            SubmitPronunciationAssessmentRequest req,
            ICommandHandler<AssessPronunciationCommand, PronunciationAssessmentResultDto> handler,
            CancellationToken ct) =>
        {
            try
            {
                var result = await handler.HandleAsync(new AssessPronunciationCommand("default-learner", req), ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapGet("/history", async (
            Guid? vocabularyId,
            int? limit,
            IQueryHandler<GetPronunciationHistoryQuery, List<PronunciationAssessmentResultDto>> handler,
            CancellationToken ct) =>
        {
            var history = await handler.HandleAsync(
                new GetPronunciationHistoryQuery("default-learner", vocabularyId, limit ?? 20),
                ct);
            return Results.Ok(history);
        });

        // Admin Rubric Endpoint
        app.MapPut("/api/admin/pronunciation/rubrics/{id:guid}", async (
            Guid id,
            UpdateScoringRubricRequest req,
            ICommandHandler<UpdateScoringRubricCommand, ScoringRubricEntity> handler,
            CancellationToken ct) =>
        {
            try
            {
                var updated = await handler.HandleAsync(new UpdateScoringRubricCommand(id, req), ct);
                return Results.Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        return app;
    }
}
