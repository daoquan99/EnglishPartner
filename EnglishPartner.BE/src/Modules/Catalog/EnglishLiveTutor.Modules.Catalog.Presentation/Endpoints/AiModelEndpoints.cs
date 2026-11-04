using EnglishLiveTutor.Api.Common;
using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.AiModels.Queries.GetPronunciationModels;
using EnglishLiveTutor.Application.Features.AiModels.Queries.GetVocabularyGenerationModels;
using EnglishLiveTutor.Application.Features.AiModels.Commands.CreateAiModelConfig;
using EnglishLiveTutor.Application.Features.AiModels.Commands.DeleteAiModelConfig;
using EnglishLiveTutor.Application.Features.AiModels.Commands.UpdateAiModelConfig;
using EnglishLiveTutor.Application.Features.AiModels.Queries.ListAiModelConfigs;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnglishLiveTutor.Api.Endpoints;

public static class AiModelEndpoints
{
    public static IEndpointRouteBuilder MapAiModelEndpoints(this IEndpointRouteBuilder app)
    {
        var modelsGroup = app.MapGroup("/api/ai-models").RequireAuthorization();

        modelsGroup.MapGet("/", async (
            HttpContext httpContext,
            string? search,
            int? page,
            int? pageSize,
            bool? paged,
            ISender sender,
            CancellationToken ct) =>
        {
            int? effectivePage = page ?? (paged == true ? 1 : null);
            var result = await sender.Send(new ListAiModelConfigsQuery(search, effectivePage, pageSize), ct);
            return result.ToHttpResult(httpContext, paged);
        });

        modelsGroup.MapPost("/", async (
            UpsertAiModelConfigRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateAiModelConfigCommand(req), ct);
            return Results.Ok(result);
        }).RequirePermission("rubrics.manage");

        modelsGroup.MapPut("/{id:guid}", async (
            Guid id,
            UpsertAiModelConfigRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateAiModelConfigCommand(id, req), ct);
            return result is null ? ApiProblem.NotFound("Không tìm thấy AI Model.") : Results.Ok(result);
        }).RequirePermission("rubrics.manage");

        modelsGroup.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            bool deleted = await sender.Send(new DeleteAiModelConfigCommand(id), ct);
            return deleted ? Results.NoContent() : ApiProblem.NotFound("Không tìm thấy AI Model.");
        }).RequirePermission("rubrics.manage");

        var pronunciationModelsGroup = app.MapGroup("/api/pronunciation").RequireAuthorization();
        pronunciationModelsGroup.MapGet("/models", async (
            ISender sender,
            CancellationToken ct) =>
        {
            var models = await sender.Send(new GetPronunciationModelsQuery(), ct);
            return Results.Ok(models);
        });

        var vocabularyModelsGroup = app.MapGroup("/api/vocabularies").RequireAuthorization();
        vocabularyModelsGroup.MapGet("/models/generation", async (
            ISender sender,
            CancellationToken ct) =>
        {
            var models = await sender.Send(new GetVocabularyGenerationModelsQuery(), ct);
            return Results.Ok(models);
        });

        return app;
    }
}
