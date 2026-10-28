using EnglishLiveTutor.Api.Common;
using EnglishLiveTutor.Api.Security;
using EnglishLiveTutor.Application.Common.Interfaces;
using EnglishLiveTutor.Application.Common.Models;
using EnglishLiveTutor.Application.Features.Vocabulary.Commands.CreateVocabulary;
using EnglishLiveTutor.Application.Features.Vocabulary.Commands.DeleteVocabulary;
using EnglishLiveTutor.Application.Features.Vocabulary.Commands.GenerateVocabulary;
using EnglishLiveTutor.Application.Features.Vocabulary.Commands.ImportVocabularyJson;
using EnglishLiveTutor.Application.Features.Vocabulary.Commands.UpdateVocabulary;
using EnglishLiveTutor.Application.Features.Vocabulary.Queries.BuildVocabularyGenerationPrompt;
using EnglishLiveTutor.Application.Features.Vocabulary.Queries.GetVocabularyFlashcard;
using EnglishLiveTutor.Application.Features.Vocabulary.Queries.GetVocabularyGenerationModels;
using EnglishLiveTutor.Application.Features.Vocabulary.Queries.ListVocabularyFlashcards;
using EnglishLiveTutor.Domain.AiCatalog;
using MediatR;

namespace EnglishLiveTutor.Api.Endpoints;

public static class VocabularyEndpoints
{
    public static IEndpointRouteBuilder MapVocabularyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/vocabularies").RequireAuthorization();

        group.MapGet("/models/generation", async (
            ISender sender,
            CancellationToken ct) =>
        {
            var models = await sender.Send(new GetVocabularyGenerationModelsQuery(), ct);
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
            ISender sender,
            CancellationToken ct) =>
        {
            int? effectivePage = page ?? (paged == true ? 1 : null);
            var result = await sender.Send(new ListVocabularyFlashcardsQuery(search, cefrLevel, topicCategory, effectivePage, pageSize), ct);
            return result.ToHttpResult(httpContext, paged);
        });

        group.MapGet("/{vocabularyId:guid}/flashcard", async (
            Guid vocabularyId,
            Guid? senseId,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(currentUser.UserId))
            {
                return ApiProblem.Unauthorized();
            }

            var dto = await sender.Send(
                new GetVocabularyFlashcardQuery(vocabularyId, senseId, currentUser.UserId!),
                ct);
            return dto is null ? ApiProblem.NotFound("Không tìm thấy flashcard từ vựng.") : Results.Ok(dto);
        });

        group.MapPost("/generate", async (
            GenerateVocabularyRequest req,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(currentUser.UserId))
            {
                return ApiProblem.Unauthorized();
            }

            string userId = currentUser.UserId!;
            var cards = await sender.Send(new GenerateVocabularyCommand(userId, req), ct);
            return Results.Ok(cards);
        }).RequireRateLimiting("ai");

        group.MapPost("/generation/prompt", async (
            BuildVocabularyGenerationPromptRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            return Results.Ok(await sender.Send(new BuildVocabularyGenerationPromptQuery(req), ct));
        });

        group.MapPost("/generation/import", async (
            ImportVocabularyJsonRequest req,
            ICurrentUser currentUser,
            ISender sender,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(currentUser.UserId))
            {
                return ApiProblem.Unauthorized();
            }

            string userId = currentUser.UserId!;
            return Results.Ok(await sender.Send(new ImportVocabularyJsonCommand(userId, req), ct));
        });

        // Admin Create Vocabulary Flashcard
        group.MapPost("/", async (
            UpsertVocabularyAdminRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateVocabularyCommand(req), ct);
            return Results.Ok(result);
        }).RequirePermission("vocabularies.manage");

        // Admin Update Vocabulary Flashcard
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpsertVocabularyAdminRequest req,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateVocabularyCommand(id, req), ct);
            return result is null ? ApiProblem.NotFound("Không tìm thấy từ vựng cần cập nhật.") : Results.Ok(result);
        }).RequirePermission("vocabularies.manage");

        // Admin Delete Vocabulary Flashcard
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            bool deleted = await sender.Send(new DeleteVocabularyCommand(id), ct);
            return deleted ? Results.NoContent() : ApiProblem.NotFound("Không tìm thấy từ vựng.");
        }).RequirePermission("vocabularies.manage");

        return app;
    }
}
