using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Application.Common.Cqrs;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.AiModels.Commands.CreateAiModelConfig;
using EnglishLiveTutor.Application.Features.AiModels.Commands.DeleteAiModelConfig;
using EnglishLiveTutor.Application.Features.AiModels.Commands.UpdateAiModelConfig;
using EnglishLiveTutor.Application.Features.AiModels.Contracts;
using EnglishLiveTutor.Application.Features.AiModels.Queries.ListAiModelConfigs;

namespace EnglishLiveTutor.Api.Endpoints;

public static class AiModelEndpoints
{
    public static IEndpointRouteBuilder MapAiModelEndpoints(this IEndpointRouteBuilder app)
    {
        var modelsGroup = app.MapGroup("/api/ai-models");

        modelsGroup.MapGet("/", async (
            HttpContext httpContext,
            string? search,
            int? page,
            int? pageSize,
            bool? paged,
            IQueryHandler<ListAiModelConfigsQuery, PagedResultDto<AiModelConfigDto>> handler,
            CancellationToken ct) =>
        {
            int? effectivePage = page ?? (paged == true ? 1 : null);
            var result = await handler.HandleAsync(new ListAiModelConfigsQuery(search, effectivePage, pageSize), ct);
            return result.ToHttpResult(httpContext, paged);
        });

        modelsGroup.MapPost("/", async (
            UpsertAiModelConfigRequest req,
            ICommandHandler<CreateAiModelConfigCommand, AiModelConfigDto> handler,
            CancellationToken ct) =>
        {
            try
            {
                var result = await handler.HandleAsync(new CreateAiModelConfigCommand(req), ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("rubrics.manage");

        modelsGroup.MapPut("/{id:guid}", async (
            Guid id,
            UpsertAiModelConfigRequest req,
            ICommandHandler<UpdateAiModelConfigCommand, AiModelConfigDto?> handler,
            CancellationToken ct) =>
        {
            try
            {
                var result = await handler.HandleAsync(new UpdateAiModelConfigCommand(id, req), ct);
                return result is null ? Results.NotFound(new { error = "Không tìm thấy AI Model." }) : Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("rubrics.manage");

        modelsGroup.MapDelete("/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteAiModelConfigCommand, bool> handler,
            CancellationToken ct) =>
        {
            bool deleted = await handler.HandleAsync(new DeleteAiModelConfigCommand(id), ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).RequirePermission("rubrics.manage");

        return app;
    }
}
