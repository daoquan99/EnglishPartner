using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Vocabulary.Commands.CreateVocabulary;
using EnglishLiveTutor.Application.Features.Vocabulary.Commands.DeleteVocabulary;
using EnglishLiveTutor.Application.Features.Vocabulary.Commands.GenerateVocabulary;
using EnglishLiveTutor.Application.Features.Vocabulary.Commands.ImportVocabularyJson;
using EnglishLiveTutor.Application.Features.Vocabulary.Queries.BuildVocabularyGenerationPrompt;
using EnglishLiveTutor.Application.Features.Vocabulary.Commands.UpdateVocabulary;
using EnglishLiveTutor.Application.Features.Vocabulary.Queries.GetVocabularyFlashcard;
using EnglishLiveTutor.Application.Features.Vocabulary.Queries.GetVocabularyGenerationModels;
using EnglishLiveTutor.Application.Features.Vocabulary.Queries.ListVocabularyFlashcards;
using EnglishLiveTutor.Domain.AiCatalog;

namespace EnglishLiveTutor.Api.Endpoints;

public static class VocabularyEndpoints
{
    public static IEndpointRouteBuilder MapVocabularyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/vocabularies");

        group.MapGet("/models/generation", async (
            IQueryHandler<GetVocabularyGenerationModelsQuery, List<AiModelConfigEntity>> handler,
            CancellationToken ct) =>
        {
            var models = await handler.HandleAsync(new GetVocabularyGenerationModelsQuery(), ct);
            return Results.Ok(models);
        });

        // List all Vocabulary Flashcards for Admin CRUD & Student Search (Database-level Pagination without arbitrary 200 cap)
        group.MapGet("/", async (
            HttpContext httpContext,
            string? search,
            string? cefrLevel,
            string? topicCategory,
            int? page,
            int? pageSize,
            bool? paged,
            IQueryHandler<ListVocabularyFlashcardsQuery, PagedResultDto<VocabularyFlashcardDto>> handler,
            CancellationToken ct) =>
        {
            int? effectivePage = page ?? (paged == true ? 1 : null);
            var result = await handler.HandleAsync(new ListVocabularyFlashcardsQuery(search, cefrLevel, topicCategory, effectivePage, pageSize), ct);
            return result.ToHttpResult(httpContext, paged);
        });

        group.MapGet("/{vocabularyId:guid}/flashcard", async (
            Guid vocabularyId,
            Guid? senseId,
            string? userId,
            IQueryHandler<GetVocabularyFlashcardQuery, VocabularyFlashcardDto?> handler,
            CancellationToken ct) =>
        {
            var dto = await handler.HandleAsync(
                new GetVocabularyFlashcardQuery(vocabularyId, senseId, userId ?? "default-learner"),
                ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        group.MapPost("/generate", async (
            GenerateVocabularyRequest req,
            ICurrentUser currentUser,
            ICommandHandler<GenerateVocabularyCommand, List<VocabularyFlashcardDto>> handler,
            CancellationToken ct) =>
        {
            try
            {
                string userId = string.IsNullOrWhiteSpace(currentUser.UserId) ? "default-learner" : currentUser.UserId!;
                var cards = await handler.HandleAsync(new GenerateVocabularyCommand(userId, req), ct);
                return Results.Ok(cards);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapPost("/generation/prompt", async (
            BuildVocabularyGenerationPromptRequest req,
            IQueryHandler<BuildVocabularyGenerationPromptQuery, VocabularyGenerationPromptDto> handler,
            CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await handler.HandleAsync(new(req), ct));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapPost("/generation/import", async (
            ImportVocabularyJsonRequest req,
            ICurrentUser currentUser,
            ICommandHandler<ImportVocabularyJsonCommand, List<VocabularyFlashcardDto>> handler,
            CancellationToken ct) =>
        {
            try
            {
                string userId = string.IsNullOrWhiteSpace(currentUser.UserId) ? "default-learner" : currentUser.UserId!;
                return Results.Ok(await handler.HandleAsync(new(userId, req), ct));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        // Admin Create Vocabulary Flashcard
        group.MapPost("/", async (
            UpsertVocabularyAdminRequest req,
            ICommandHandler<CreateVocabularyCommand, VocabularyFlashcardDto> handler,
            CancellationToken ct) =>
        {
            try
            {
                var result = await handler.HandleAsync(new CreateVocabularyCommand(req), ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("vocabularies.manage");

        // Admin Update Vocabulary Flashcard
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpsertVocabularyAdminRequest req,
            ICommandHandler<UpdateVocabularyCommand, VocabularyFlashcardDto?> handler,
            CancellationToken ct) =>
        {
            try
            {
                var result = await handler.HandleAsync(new UpdateVocabularyCommand(id, req), ct);
                return result is null ? Results.NotFound(new { error = "Không tìm thấy từ vựng cần cập nhật." }) : Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequirePermission("vocabularies.manage");

        // Admin Delete Vocabulary Flashcard
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteVocabularyCommand, bool> handler,
            CancellationToken ct) =>
        {
            bool deleted = await handler.HandleAsync(new DeleteVocabularyCommand(id), ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).RequirePermission("vocabularies.manage");

        return app;
    }
}
